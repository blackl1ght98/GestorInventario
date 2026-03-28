using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaymentRepository:IPaymentRepository
    {
        public readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalRepository> _logger;
        private readonly IEmailService _emailService;
        private readonly IConversionUtils _conversion;
        public PaymentRepository(GestorInventarioContext context, ILogger<PaypalRepository> logger, IEmailService email, IConversionUtils conversion)
        {
            _context = context;
            _logger = logger;
            _emailService = email;
            _conversion = conversion;
        }
      
        public async Task<OperationResult<Pedido>> ObtenerNumeroPedido(RefundFormViewModel form)
        {
            var numeroPedido= await _context.Pedidos.FirstOrDefaultAsync(p => p.NumeroPedido == form.NumeroPedido);
            if(numeroPedido is null)
            {
                return OperationResult<Pedido>.Fail("El numero de pedido no se encuentra");
            }
            return OperationResult<Pedido>.Ok("", numeroPedido);
        }
        public async Task<OperationResult<Pedido>> AgregarInfoPedido(int usuarioActual, string? captureId, string? total, string? currency, string? orderId)
        {
            // Validar parámetros de entrada
            if (string.IsNullOrWhiteSpace(captureId) || string.IsNullOrWhiteSpace(total) ||
                string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("Parámetros inválidos en AgregarInfoPedido: usuarioActual={UsuarioId}, captureId={CaptureId}, total={Total}, currency={Currency}, orderId={OrderId}",
                    usuarioActual, captureId, total, currency, orderId);
                return OperationResult<Pedido>.Fail("Datos no validos");
            }

            var pedido = await _context.Pedidos
                .Where(p => p.IdUsuario == usuarioActual && p.EstadoPedido == "En Proceso")
                .OrderByDescending(p => p.FechaPedido)
                .FirstOrDefaultAsync();

            if (pedido == null)
            {
                _logger.LogWarning("No se encontró un pedido en proceso para el usuario {UsuarioId}", usuarioActual);
                return OperationResult<Pedido>.Fail("Pedido no encontrado para el usuario especificado");
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                pedido.CaptureId = captureId;
                pedido.Total = total;
                pedido.Currency = currency;
                pedido.OrderId = orderId;
                pedido.EstadoPedido = "Pagado";

                await _context.UpdateEntityAsync(pedido);
                await transaction.CommitAsync();
                return OperationResult<Pedido>.Ok("",pedido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el pedido para usuario {UsuarioId}, captureId={CaptureId}", usuarioActual, captureId);
                await transaction.RollbackAsync();
                return OperationResult<Pedido>.Fail("Ocurrio un error al agregar el pedido");
            }
        }
        public OperationResult<PayPalPaymentDetail> ProcesarDetallesSuscripcion(OrderDetailsResponse detallespago)
        {
            if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
            {
                _logger.LogInformation("No se encuentran las unidades de pago en la peticion");
                throw new InvalidOperationException("No se encuentran las unidades de pago en la peticion");
            }

            var firstPurchaseUnit = detallespago.PurchaseUnits.First();

            var detallesSuscripcion = new PayPalPaymentDetail
            {
                Id = detallespago.Id,
                Intent = detallespago.Intent,
                Status = detallespago.Status,
                PaymentMethod = "paypal",
                PayerEmail = detallespago.Payer?.Email,
                PayerFirstName = detallespago.Payer?.Name?.GivenName,
                PayerLastName = detallespago.Payer?.Name?.Surname,
                PayerId = detallespago.Payer?.PayerId,
                ShippingRecipientName = firstPurchaseUnit?.Shipping?.Name?.FullName,
                ShippingLine1 = firstPurchaseUnit?.Shipping?.Address?.AddressLine1,
                ShippingCity = firstPurchaseUnit?.Shipping?.Address?.AdminArea2,
                ShippingState = firstPurchaseUnit?.Shipping?.Address?.AdminArea1,
                ShippingPostalCode = firstPurchaseUnit?.Shipping?.Address?.PostalCode,
                ShippingCountryCode = firstPurchaseUnit?.Shipping?.Address?.CountryCode,
            };

            // Procesar el amount
            if (firstPurchaseUnit?.Amount != null)
            {
                detallesSuscripcion.AmountTotal = _conversion.ConvertToDecimal(firstPurchaseUnit.Amount.Value);
                detallesSuscripcion.AmountCurrency = firstPurchaseUnit.Amount.CurrencyCode;

                if (firstPurchaseUnit.Amount.Breakdown != null)
                {
                    detallesSuscripcion.AmountItemTotal = _conversion.ConvertToDecimal(
                        firstPurchaseUnit.Amount.Breakdown.ItemTotal?.Value ?? "0");
                    detallesSuscripcion.AmountShipping = _conversion.ConvertToDecimal(
                        firstPurchaseUnit.Amount.Breakdown.Shipping?.Value ?? "0");
                }
            }

            // Procesar payee
            if (firstPurchaseUnit?.Payee != null)
            {
                detallesSuscripcion.PayeeMerchantId = firstPurchaseUnit.Payee.MerchantId;
                detallesSuscripcion.PayeeEmail = firstPurchaseUnit.Payee.EmailAddress;
            }

            detallesSuscripcion.Description = firstPurchaseUnit?.Description;

            // Procesar pagos y capturas
            if (firstPurchaseUnit?.Payments?.Captures != null && firstPurchaseUnit.Payments.Captures.Any())
            {
                var firstCapture = firstPurchaseUnit.Payments.Captures.First();
                detallesSuscripcion.SaleId = firstCapture.Id;
                detallesSuscripcion.CaptureStatus = firstCapture.Status;

                if (firstCapture.Amount != null)
                {
                    detallesSuscripcion.CaptureAmount = _conversion.ConvertToDecimal(firstCapture.Amount.Value);
                    detallesSuscripcion.CaptureCurrency = firstCapture.Amount.CurrencyCode;
                }

                if (firstCapture.SellerProtection != null)
                {
                    detallesSuscripcion.ProtectionEligibility = firstCapture.SellerProtection.Status;
                }

                // Procesar seller receivable breakdown con verificaciones de nulidad
                if (firstCapture.SellerReceivableBreakdown != null)
                {
                    detallesSuscripcion.TransactionFeeAmount = firstCapture.SellerReceivableBreakdown.PaypalFee != null ?
                        _conversion.ConvertToDecimal(firstCapture.SellerReceivableBreakdown.PaypalFee.Value) : 0;

                    detallesSuscripcion.TransactionFeeCurrency = firstCapture.SellerReceivableBreakdown.PaypalFee?.CurrencyCode;

                    detallesSuscripcion.ReceivableAmount = firstCapture.SellerReceivableBreakdown.NetAmount != null ?
                        _conversion.ConvertToDecimal(firstCapture.SellerReceivableBreakdown.NetAmount.Value) : 0;

                    detallesSuscripcion.ReceivableCurrency = firstCapture.SellerReceivableBreakdown.NetAmount?.CurrencyCode;

                    if (firstCapture.SellerReceivableBreakdown.ExchangeRate != null &&
                        !string.IsNullOrEmpty(firstCapture.SellerReceivableBreakdown.ExchangeRate.Value))
                    {
                        if (decimal.TryParse(firstCapture.SellerReceivableBreakdown.ExchangeRate.Value,
                            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                        {
                            detallesSuscripcion.ExchangeRate = exchangeRate;
                        }
                    }
                }
            }

            return OperationResult<PayPalPaymentDetail>.Ok("", detallesSuscripcion);
        }
        public async Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion,int usuarioActual, RefundFormViewModel form,Pedido obtenerNumeroPedido, string emailCliente)
        {
            // Lista para almacenar los ítems de PayPal
            var paypalItems = new List<PayPalPaymentItem>();

            // Procesar items
            if (firstPurchaseUnit?.Items != null)
            {
                foreach (var item in firstPurchaseUnit.Items)
                {
                    var paymentItem = new PayPalPaymentItem
                    {
                        PayPalId = detallesSuscripcion.Id,
                        ItemName = item.Name,
                        ItemSku = item.Sku,
                        ItemPrice = item.UnitAmount != null ? _conversion.ConvertToDecimal(item.UnitAmount.Value) : 0,
                        ItemCurrency = item.UnitAmount?.CurrencyCode,
                        ItemTax = item.Tax != null ? _conversion.ConvertToDecimal(item.Tax.Value) : 0,
                        ItemQuantity = _conversion.ConvertToInt(item.Quantity)
                    };

                    paypalItems.Add(paymentItem);
                }
            }

            // Crear el reembolso
            var rembolso = new Rembolso
            {
                UsuarioId = usuarioActual,
                NumeroPedido = form.NumeroPedido,
                NombreCliente = form.NombreCliente,
                EmailCliente = emailCliente,
                FechaRembolso = form.FechaRembolso,
                EstadoRembolso = "EN REVISION PARA APROBACION",
                MotivoRembolso = form.MotivoRembolso,
                PedidoId = obtenerNumeroPedido.Id,
            };

            await _context.AddEntityAsync(rembolso);

            
            var emailRembolso = new EmailRembolsoDto
            {
                NumeroPedido = rembolso.NumeroPedido,
                NombreCliente = rembolso.NombreCliente,
                EmailCliente = rembolso.EmailCliente,
                FechaRembolso = rembolso.FechaRembolso,
                MotivoRembolso = rembolso.MotivoRembolso,
                Productos = paypalItems
            };
            await  _emailService.EnviarEmailSolicitudRembolso(emailRembolso);

            if (!paypalItems.Any())
            {
                _logger.LogError($"No se encontraron ítems en PurchaseUnits para el reembolso del pedido {form.NumeroPedido}.");
                return OperationResult<PayPalPaymentItem>.Fail("No se puede procesar el rembolso sin items asociados");
            }
            return OperationResult<PayPalPaymentItem>.Ok("",paypalItems.First());

        }
        public async Task LimpiarPedidoCorruptoUsuarioAsync(int userId)
        {
            // Buscar SOLO UN pedido pendiente/corrupto del usuario
            var pedidoCorrupto = await _context.Pedidos.Include(x=>x.DetallePedidos).Where(p=>p.IdUsuario == userId && p.EstadoPedido=="En Proceso"&& string.IsNullOrEmpty(p.CaptureId)).FirstOrDefaultAsync();

            if (pedidoCorrupto != null)
            {
                // Opcional: verificar que efectivamente es un carrito/checkout abandonado
                if (pedidoCorrupto.EsCarrito == false) 
                {
                    _context.DetallePedidos.RemoveRange(pedidoCorrupto.DetallePedidos);
                    _context.Pedidos.Remove(pedidoCorrupto);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Limpieza automática: pedido corrupto eliminado para usuario {UserId}. ID: {PedidoId}, Fecha: {FechaPedido}",
                        userId, pedidoCorrupto.Id, pedidoCorrupto.FechaPedido);
                }
            }
        }

    }
}
