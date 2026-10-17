using GestorInventario.Application.DTOS;
using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
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
    private readonly IPedidoRepository _pedidoRepository;

    public OrderService(ILogger<OrderService> logger, IPedidoRepository pedido,
        IPaypalOrderService paypal, IPaymentRepository payment)
    {
        _logger = logger;
        _pedidoRepository = pedido;
        _paypalOrderService = paypal;
        _paymentRepository = payment;
    }

    public async Task<OperationResult<string>> EliminarPedido(int id)
    {
        var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(id);
        if (pedido == null)
            return OperationResult<string>.Fail();

        // Solo carritos sin capturas de PayPal
        if (pedido.EstadoPedido == EstadoPedido.Carrito.ToString()
            && !pedido.PayPalPaymentCaptures.Any())
        {
            await _pedidoRepository.EliminarCarritoAsync(pedido);
            return OperationResult<string>.Ok("Carrito eliminado");
        }

        return OperationResult<string>.Fail("No se puede eliminar un pedido con historial");
    }

    public async Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(
        string id, int pedidoId)
    {
        var detallesPayPal = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(id);

        if (!detallesPayPal.Success)
            return OperationResult<PayPalPaymentDetail>.Fail(detallesPayPal.Message);

        var detalles = detallesPayPal.Unwrap(); // PayPalOrderDetailsDto

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
        int usuarioActual,
        string captureId,
        decimal total,
        string? currency,
        string orderId)
    {
        if (string.IsNullOrWhiteSpace(captureId) ||
            string.IsNullOrWhiteSpace(currency) ||
            string.IsNullOrWhiteSpace(orderId))
        {
            _logger.LogWarning("Parámetros inválidos al confirmar pago: captureId, currency u orderId vacíos");
            return OperationResult<Pedido>.Fail();
        }


        var pedido = await _pedidoRepository.ObtenerPedidoPendienteUsuarioAsync(usuarioActual);
        if (pedido == null)
        {
            _logger.LogWarning("No se encontró pedido pendiente para el usuario {UsuarioId}", usuarioActual);
            return OperationResult<Pedido>.Fail();
        }


        var paymentDetail = await _paymentRepository.ObtenerDetallesPago(orderId);

        if (paymentDetail == null)
        {
            paymentDetail = new PayPalPaymentDetail
            {
                Id = orderId,
                Intent = "CAPTURE",
                AmountTotal = total,
                AmountCurrency = currency,
                AmountItemTotal = total,
                AmountShipping = 0,
                Description = $"Pedido #{pedido.NumeroPedido} - pendiente de sincronización con PayPal",
                CreateTime = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow
            };
            await _paymentRepository.AgregarDetallePagoAsync(paymentDetail);
        }


        var capturePayment = new PayPalPaymentCapture
        {
            PaymentId = orderId,
            CaptureId = captureId,
            PedidoId = pedido.Id,
            Amount = total,
            Currency = currency,
            TransactionFeeAmount = 0,
            ReceivableAmount = 0,
            ExchangeRate = 0,
            FinalCapture = false,
            CreateTime = DateTime.UtcNow,
            UpdateTime = DateTime.UtcNow
        };

        await _paymentRepository.AgregarCaptureAsync(capturePayment);


        pedido.Total = total;
        pedido.Currency = currency;
        pedido.EstadoPedido = EstadoPedido.Pagado.ToString();

        await _pedidoRepository.ActualizarPedidoAsync(pedido);

        return OperationResult<Pedido>.Ok(pedido, "Pago confirmado. Pendiente de sincronización con PayPal.");
    }

    public async Task AddInfoTrackingOrder(int pedidoId, string tracking, string carrier)
    {
        var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
        if (pedido == null)
            throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");
        pedido.EstadoPedido = EstadoPedido.Enviado.ToString();
        pedido.TrackingNumber = tracking;
        pedido.UrlTracking = "URL NO ESPECIFICADA";
        pedido.Transportista = carrier;
        await _pedidoRepository.ActualizarPedidoAsync(pedido);
    }

    /// <summary>
    ///     Busca el detalle en BD. Si no existe, lo crea.
    ///     Si existe, lo elimina y reutiliza la instancia.
    /// </summary>
    private async Task<PayPalPaymentDetail> ObtenerOCrearDetallePagoAsync(
        string id, PayPalOrderDetailsDto detalles)
    {
        var existente = await _paymentRepository.ObtenerDetallesPago(id);

        if (existente == null)
        {
            var nuevo = new PayPalPaymentDetail { Id = detalles.OrderId };
            await _paymentRepository.AgregarDetallePagoAsync(nuevo);
            _logger.LogInformation("Detalle de pago {Id} creado en BD", id);
            return nuevo;
        }

        await _paymentRepository.EliminarDetallesPagoAsync(existente);
        _logger.LogInformation("Detalle de pago {Id} actualizado en BD", id);
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
    ///     Procesa los items del pedido.
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
}