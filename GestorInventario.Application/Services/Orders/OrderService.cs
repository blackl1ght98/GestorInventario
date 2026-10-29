using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Domain.Common;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Services.Orders;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaypalOrderService _paypalOrderService;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public OrderService(ILogger<OrderService> logger, IOrderRepository order,
        IPaypalOrderService paypal, IPaymentRepository payment, ICurrentUserAccessor currentUserAccessor)
    {
        _logger = logger;
        _orderRepository = order;
        _paypalOrderService = paypal;
        _paymentRepository = payment;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<OperationResult<string>> EliminarPedido(int id)
    {
        var pedido = await _orderRepository.ObtenerPedidoConDetallesAsync(id);
        if (pedido == null)
            return OperationResult<string>.Fail();

        // Solo carritos sin capturas de PayPal
        if (pedido.EstadoPedido == EstadoPedido.Carrito.ToString()
            && !pedido.PayPalPaymentCaptures.Any())
        {
            await _orderRepository.EliminarCarritoAsync(pedido);
            return OperationResult<string>.Ok("Carrito eliminado");
        }

        return OperationResult<string>.Fail("No se puede eliminar un order con historial");
    }

    public async Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(
        string id, int pedidoId)
    {
        var detallesPayPal = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(id);

        if (!detallesPayPal.Success)
            return OperationResult<PayPalPaymentDetail>.Fail(detallesPayPal.Message);

        var detalles = detallesPayPal.Unwrap();

        var detalleBD = await ObtenerOCrearDetallePagoAsync(id, detalles);

        // Payer
        detalleBD.PayerEmail = detalles.PayerEmail;
        detalleBD.PayerFirstName = detalles.PayerFirstName;
        detalleBD.PayerLastName = detalles.PayerLastName;
        detalleBD.PayerId = detalles.PayerId;
        detalleBD.Intent = detalles.Intent;
        detalleBD.OrderStatus = detalles.Status;
        detalleBD.Description = detalles.Description;
        detalleBD.PayeeMerchantId = detalles.PayeeMerchantId;
        detalleBD.PayeeEmail = detalles.PayeeEmail;

        // Montos
        detalleBD.AmountTotal = detalles.AmountTotal;
        detalleBD.AmountCurrency = detalles.AmountCurrency;
        detalleBD.AmountItemTotal = detalles.AmountItemTotal;
        detalleBD.AmountShipping = detalles.AmountShipping;

        await ProcesarShippingAsync(detalles, detalleBD);
        await ProcesarCapturesAsync(detalles, detalleBD, pedidoId);
        await ProcesarRefundsAsync(detalles, detalleBD, pedidoId);
        await ProcesarItemsAsync(detalles, detalleBD);

        return OperationResult<PayPalPaymentDetail>.Ok(detalleBD);
    }


    public async Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(
        PaymentCapturedDto capture,
        string paymentId)
    {
        if (string.IsNullOrWhiteSpace(capture.CaptureId) ||
            string.IsNullOrWhiteSpace(capture.Currency) ||
            string.IsNullOrWhiteSpace(paymentId))
        {
            _logger.LogWarning("Parámetros inválidos al confirmar pago: captureId, currency u paymentId vacíos");
            return OperationResult<Pedido>.Fail();
        }

        int userId = _currentUserAccessor.GetCurrentUserId();

        var pedido = await _orderRepository.ObtenerPedidoPendienteUsuarioAsync(userId);
        if (pedido == null)
        {
            _logger.LogWarning("No se encontró order pendiente para el usuario {UsuarioId}", userId);
            return OperationResult<Pedido>.Fail();
        }


        var paymentDetail = await _paymentRepository.ObtenerDetallesPago(paymentId);

        if (paymentDetail == null)
        {
            paymentDetail = new PayPalPaymentDetail
            {
                Id = paymentId,
                Intent = "CAPTURE",
                AmountTotal = capture.Total,
                AmountCurrency = capture.Currency,
                AmountItemTotal = capture.Total,
                AmountShipping = 0,
                Description = $"Pedido #{pedido.NumeroPedido} - pendiente de sincronización con PayPal",
                CreateTime = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow
            };
            await _paymentRepository.AgregarDetallePagoAsync(paymentDetail);
        }


        var capturePayment = new PayPalPaymentCapture
        {
            PaymentId = paymentId,
            CaptureId = capture.CaptureId,
            PedidoId = pedido.Id,
            Amount = capture.Total,
            Currency = capture.Currency,
            TransactionFeeAmount = 0,
            ReceivableAmount = 0,
            ExchangeRate = 0,
            FinalCapture = false,
            CreateTime = DateTime.UtcNow,
            UpdateTime = DateTime.UtcNow
        };

        await _paymentRepository.AgregarCaptureAsync(capturePayment);


        pedido.Total = capture.Total;
        pedido.Currency = capture.Currency;
        pedido.EstadoPedido = EstadoPedido.Pagado.ToString();

        await _orderRepository.ActualizarPedidoAsync(pedido);

        return OperationResult<Pedido>.Ok(pedido, "Pago confirmado. Pendiente de sincronización con PayPal.");
    }

    public async Task AddInfoTrackingOrder(int pedidoId, string tracking, string carrier)
    {
        var pedido = await _orderRepository.ObtenerPedidoPorIdAsync(pedidoId);
        if (pedido == null)
            throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");
        pedido.EstadoPedido = EstadoPedido.Enviado.ToString();
        pedido.TrackingNumber = tracking;
        pedido.UrlTracking = "URL NO ESPECIFICADA";
        pedido.Transportista = carrier;
        await _orderRepository.ActualizarPedidoAsync(pedido);
    }

    /// <summary>
    ///     Busca el detalle en BD. Si no existe, lo crea.
    ///     Si existe, lo elimina y reutiliza la instancia.
    /// </summary>
    private async Task<PayPalPaymentDetail> ObtenerOCrearDetallePagoAsync(
        string id, PayPalOrderDetailsDto detalles)
    {
        // Buscamos si ya existe por su ID de PayPal
        var existente = await _paymentRepository.ObtenerDetallesPago(id);

        if (existente == null)
        {
            var nuevo = new PayPalPaymentDetail { Id = detalles.OrderId };
            await _paymentRepository.AgregarDetallePagoAsync(nuevo);
            _logger.LogInformation("Detalle de pago {Id} creado en BD", id);
            return nuevo;
        }

        // SI YA EXISTE: NO LO BORRES. Solo actualiza sus propiedades.
        // De esta forma la entidad padre NUNCA pierde su identidad ni rompe relaciones.
        _logger.LogInformation("Detalle de pago {Id} ya existe. Actualizando datos...", id);
        return existente;
    }

    /// <summary>
    ///     Crea el registro de información de envío.
    /// </summary>
    private async Task ProcesarShippingAsync(
        PayPalOrderDetailsDto detalles, PayPalPaymentDetail detallePago)
    {
        if (detalles.Shipping == null)
            return;

        var payerName = $"{detallePago.PayerFirstName} {detallePago.PayerLastName}".Trim();

        var envio = new PayPalPaymentShipping
        {
            PaymentId = detallePago.Id,
            RecipientName = detalles.Shipping.RecipientName ?? payerName,
            AddressLine1 = detalles.Shipping.AddressLine1,
            City = detalles.Shipping.City,
            State = detalles.Shipping.State,
            PostalCode = detalles.Shipping.PostalCode,
            CountryCode = detalles.Shipping.CountryCode
        };

        await _paymentRepository.AgregarInfoEnvioAsync(envio);
    }

    /// <summary>
    ///     Procesa las capturas de pago (captures) de PayPal.
    /// </summary>
    private async Task ProcesarCapturesAsync(
        PayPalOrderDetailsDto detalles,
        PayPalPaymentDetail detallePago,
        int pedidoId)
    {
        if (detalles.Captures == null || !detalles.Captures.Any())
            return;

        foreach (var capture in detalles.Captures)
        {
            var paypalCapture = new PayPalPaymentCapture
            {
                PaymentId = detallePago.Id,
                CaptureId = capture.CaptureId,
                Status = capture.Status,
                Amount = capture.Amount,
                Currency = capture.Currency,
                FinalCapture = capture.FinalCapture,
                ProtectionEligibility = capture.ProtectionEligibility,
                TransactionFeeAmount = capture.TransactionFeeAmount,
                TransactionFeeCurrency = capture.TransactionFeeCurrency,
                ReceivableAmount = capture.ReceivableAmount,
                ReceivableCurrency = capture.ReceivableCurrency,
                ExchangeRate = capture.ExchangeRate.ToDecimalSafe(),
                DisputeCategories = capture.DisputeCategoriesJson,
                CreateTime = capture.CreateTime.ToDateTimeSafe(),
                UpdateTime = capture.UpdateTime.ToDateTimeSafe(),
                PedidoId = pedidoId
                // Añade InvoiceId u otros campos si tu entidad los tiene
            };

            await _paymentRepository.AgregarCaptureAsync(paypalCapture);
        }
    }

    /// <summary>
    ///     Procesa los reembolsos (refunds) de PayPal.
    /// </summary>
    private async Task ProcesarRefundsAsync(
        PayPalOrderDetailsDto detalles,
        PayPalPaymentDetail detallePago,
        int pedidoId)
    {
        if (detalles.Refunds == null || !detalles.Refunds.Any())
            return;

        foreach (var refund in detalles.Refunds)
        {
            var paypalRefund = new PayPalPaymentRefund // o el nombre real de tu entidad
            {
                PaymentId = detallePago.Id,
                RefundId = refund.RefundId,
                Status = refund.Status,
                Amount = refund.Amount,
                Currency = refund.Currency,
                NoteToPayer = refund.NoteToPayer,

                CreateTime = refund.CreateTime.ToDateTimeSafe(),
                UpdateTime = refund.UpdateTime.ToDateTimeSafe(),
                PedidoId = pedidoId
            };

            await _paymentRepository.AgregarRefundAsync(paypalRefund);
        }
    }

    /// <summary>
    ///     Procesa los items del order.
    /// </summary>
    private async Task ProcesarItemsAsync(
        PayPalOrderDetailsDto detalles, PayPalPaymentDetail detallePago)
    {
        if (detalles.Items == null || !detalles.Items.Any())
            return;

        foreach (var item in detalles.Items)
        {
            var paymentItem = new PayPalPaymentItem
            {
                PayPalId = detallePago.Id,
                ItemName = item.Name,
                ItemSku = item.Sku,
                ItemPrice = item.UnitAmount,
                ItemCurrency = item.Currency,
                ItemTax = item.Tax,
                ItemQuantity = item.Quantity
            };

            await _paymentRepository.AgregarPagoItemAsync(paymentItem);
        }
    }

    public async Task<OperationResult<DetalleOrdenDto>> GetOrderDetailsAsync(int id)
    {
        var pedido = await _orderRepository.ObtenerPedidoConDetallesAsync(id);
        if (pedido == null)
        {
            return OperationResult<DetalleOrdenDto>.Fail();
        }

        var detalles = pedido.DetallePedidos?.ToList() ?? new List<DetallePedido>();
        if (pedido.EsCarrito && !detalles.Any())
        {
            return OperationResult<DetalleOrdenDto>.Fail("El carrito está vacío.");
        }

        var productos = detalles.Select(d =>
        {
            var precioUnitario = d.Producto?.Precio ?? 0;
            var (subtotalSinIva, iva, totalConIva) =
                CalculadoraFiscal.CalcularCosteProducto(precioUnitario, d.Cantidad);

            return new DetallePedidoDto
            {
                DetalleId = d.Id,
                NombreProducto = d.Producto?.NombreProducto ?? "Producto sin nombre",
                Descripcion = d.Producto?.Descripcion,
                Cantidad = d.Cantidad,
                PrecioUnitario = precioUnitario,
                SubtotalSinIva = subtotalSinIva,
                Iva = iva,
                Total = totalConIva,
                Rembolsado = d.Rembolsado
            };
        }).ToList();

        var (totalSinIva, totalIva, granTotal) = CalculadoraFiscal.CalcularTotales(
            detalles.Select(d => (d.Producto?.Precio ?? 0, d.Cantidad))
        );

        // BLINDAJE: Evita NullReferenceException si no hay pagos de PayPal asociados
        var paymentId = pedido.PayPalPaymentCaptures?.FirstOrDefault()?.PaymentId;

        var viewmodel = new DetalleOrdenDto
        {
            FechaPedido = pedido.FechaPedido,
            NombreCompleto = pedido.IdUsuarioNavigation?.NombreCompleto ?? "Cliente no registrado",
            NumeroPedido = pedido.NumeroPedido,
            EstadoPedido = pedido.EstadoPedido,
            Currency = pedido.Currency,
            DetallePedidoDtos = productos,
            TotalSinIva = totalSinIva,
            TotalIva = totalIva,
            Total = granTotal,
            PaymentId = paymentId,
            PedidoId = pedido.Id,
        };

        return OperationResult<DetalleOrdenDto>.Ok(viewmodel);
    }
}