
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;
using GestorInventario.Infraestructure.Repositories.PaypalRepository;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels.Pedidos;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class PedidoManagementService: IPedidoManagementService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ILogger<PedidoManagementService> _logger;
        private readonly ICurrentUserAccessor _currentUserAccesor;
        private readonly IConversionUtils _conversion;
        private readonly IPaypalOrderService _paypalOrder;
        private readonly IPaymentRepository _payment;
        private readonly IPaypalRepository _paypalRepository;
        public PedidoManagementService(ILogger<PedidoManagementService> logger,  IPedidoRepository pedido, ICurrentUserAccessor current,
            IConversionUtils conversion, IPaypalOrderService paypal, IPaymentRepository payment, IPaypalRepository paypalRepository)
        {
            
            _logger = logger;
            _pedidoRepository = pedido;
            _currentUserAccesor = current;
            _conversion = conversion;
            _paypalOrder = paypal;
            _payment = payment;
            _paypalRepository=paypalRepository;
        
        }
        public async Task<OperationResult<string>> EliminarPedido(int id)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(id);
            if (pedido == null)
                return OperationResult<string>.Fail("Pedido no encontrado");

            // Solo carritos sin capturas de PayPal
            if (pedido.EstadoPedido == EstadoPedido.Carrito.ToString()
                && !pedido.PayPalPaymentCaptures.Any())
            {
                await _pedidoRepository.EliminarCarritoAsync(pedido);
                return OperationResult<string>.Ok("Carrito eliminado");
            }

            return OperationResult<string>.Fail("No se puede eliminar un pedido con historial");
        }
      
        public async Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(string id, int pedidoId)
        {
            var detalles = await _paypalOrder.ObtenerDetallesPagoEjecutadoAsync(id);
            if (detalles == null)
                return OperationResult<PayPalPaymentDetail>.Fail("Detalles del pedido no encontrados para generar la factura");

            var existingDetail = await _payment.ObtenerDetallesPago(id);
            PayPalPaymentDetail detallesPago;

            if (existingDetail == null)
            {
                // Crear nuevo registro
                detallesPago = new PayPalPaymentDetail { Id = detalles.Id };
                await _payment.AgregarDetallePagoAsync(detallesPago);
                _logger.LogInformation("Detalle de pago {Id} creado en BD", id);
            }
            else
            {
                // Limpiar datos relacionados para reinsertar frescos
                await _payment.EliminarDetallesPagoAsync(existingDetail);
              
                detallesPago = existingDetail;
                _logger.LogInformation("Detalle de pago {Id} actualizado en BD", id);
            }

            // Actualizar campos principales
            detallesPago.Intent = detalles.Intent;
            detallesPago.OrderStatus = detalles.Status;
            detallesPago.PayerEmail = detalles.Payer.Email;
            detallesPago.PayerFirstName = detalles.Payer.Name.GivenName;
            detallesPago.PayerLastName = detalles.Payer.Name.Surname;
            detallesPago.PayerId = detalles.Payer.PayerId;

            // Información de envío
            var firstUnit = detalles.PurchaseUnits?.FirstOrDefault();
            if (firstUnit != null)
            {
                var informacionEnvio = new PayPalPaymentShipping
                {
                    PaymentId = detalles.Id,
                    RecipientName = firstUnit.Shipping.Name.FullName,
                    AddressLine1 = firstUnit.Shipping.Address.AddressLine1,
                    City = firstUnit.Shipping.Address.AdminArea2,
                    State = firstUnit.Shipping.Address.AdminArea1,
                    PostalCode = firstUnit.Shipping.Address.PostalCode,
                    CountryCode = firstUnit.Shipping.Address.CountryCode
                };
                await _payment.AgregarInfoEnvioAsync(informacionEnvio);

                // Montos
                detallesPago.AmountTotal = _conversion.ConvertToDecimal(firstUnit.Amount.Value);
                detallesPago.AmountCurrency = firstUnit.Amount.CurrencyCode;
                detallesPago.AmountItemTotal = _conversion.ConvertToDecimal(firstUnit.Amount.Breakdown.ItemTotal.Value);

                // Calcular subtotal si es necesario
                if (detallesPago.AmountItemTotal == 0 && firstUnit.Items != null)
                {
                    detallesPago.AmountItemTotal = firstUnit.Items.Sum(item =>
                        _conversion.ConvertToDecimal(item.UnitAmount.Value.ToString()) *
                        _conversion.ConvertToInt(item.Quantity.ToString()));
                }

                detallesPago.AmountShipping = _conversion.ConvertToDecimal(firstUnit.Amount.Breakdown.Shipping.Value);
                detallesPago.PayeeMerchantId = firstUnit.Payee.MerchantId;
                detallesPago.PayeeEmail = firstUnit.Payee.EmailAddress;
                detallesPago.Description = firstUnit.Description;

               

                // Captures
                if (firstUnit.Payments?.Captures != null)
                {
                    foreach (var capture in firstUnit.Payments.Captures.Where(c => c != null))
                    {
                        

                        // Solo crear si no existe
                        var paypalCapture = new PayPalPaymentCapture
                        {
                            PaymentId = detallesPago.Id,
                            CaptureId = capture.Id,
                            Status = capture.Status,
                            PedidoId = pedidoId,           
                            Amount = _conversion.ConvertToDecimal(capture.Amount.Value),
                            Currency = capture.Amount.CurrencyCode,
                            ProtectionEligibility = capture.SellerProtection.Status,
                            TransactionFeeAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown.PaypalFee.Value),
                            TransactionFeeCurrency = capture.SellerReceivableBreakdown.PaypalFee.CurrencyCode,
                            ReceivableAmount = _conversion.ConvertToDecimal(capture.SellerReceivableBreakdown.NetAmount.Value),
                            ReceivableCurrency = capture.SellerReceivableBreakdown.NetAmount.CurrencyCode,
                            FinalCapture = capture.FinalCapture,
                            CreateTime = _conversion.ConvertToDateTime(capture.CreateTime),
                            UpdateTime = _conversion.ConvertToDateTime(capture.UpdateTime),
                        };

                        var exchangeValue = capture.SellerReceivableBreakdown?.ExchangeRate?.Value;

                        if (string.IsNullOrEmpty(exchangeValue))
                        {
                            paypalCapture.ExchangeRate = 0;
                        }
                        else if (decimal.TryParse(exchangeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal exchangeRate))
                        {
                            paypalCapture.ExchangeRate = exchangeRate;
                        }
                        else
                        {
                            paypalCapture.ExchangeRate = 0;
                        }


                        if (capture.SellerProtection.DisputeCategories != null)
                            paypalCapture.DisputeCategories = JsonConvert.SerializeObject(capture.SellerProtection.DisputeCategories);

                        await _payment.AgregarCaptureAsync(paypalCapture);
                    }
                }

                // Items
                if (firstUnit.Items != null)
                {
                    foreach (var item in firstUnit.Items)
                    {
                        var paymentItem = new PayPalPaymentItem
                        {
                            PayPalId = detallesPago.Id,
                            ItemName = item.Name,
                            ItemSku = item.Sku,
                            ItemPrice = _conversion.ConvertToDecimal(item.UnitAmount.Value),
                            ItemCurrency = item.UnitAmount.CurrencyCode,
                            ItemTax = _conversion.ConvertToDecimal(item.Tax.Value),
                            ItemQuantity = _conversion.ConvertToInt(item.Quantity)
                        };
                        await _payment.AgregarPagoItemAsync(paymentItem);
                    }
                }
            }

            return OperationResult<PayPalPaymentDetail>.Ok("", detallesPago);
        }


        public async Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(
           int usuarioActual,
           string captureId,
           decimal total,
           string? currency,
           string orderId)
        {
            // 1. Validar parámetros
            if (string.IsNullOrWhiteSpace(captureId) ||
                string.IsNullOrWhiteSpace(currency) ||
                string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("Parámetros inválidos...");
                return OperationResult<Pedido>.Fail("Datos no validos");
            }

            // 2. Buscar pedido
            var pedido = await _pedidoRepository.ObtenerPedidoPendienteUsuarioAsync(usuarioActual);
            if (pedido == null)
            {
                _logger.LogWarning("No se encontró pedido...");
                return OperationResult<Pedido>.Fail("Pedido no encontrado");
            }

            
            var paymentDetail = await _payment.ObtenerDetallesPago(orderId);

            if (paymentDetail == null)
            {
                paymentDetail = new PayPalPaymentDetail
                {
                    Id = orderId,           
                    Intent = "CAPTURE",
                    OrderStatus = "COMPLETED",
                    PayeeEmail="email no establecido",
                    PayerFirstName="no establecido",
                    PayerLastName="no establecido",
                    PayerId="no establecido",
                    AmountTotal = total,
                    AmountCurrency = currency,
                    AmountItemTotal=0,
                    AmountShipping=0,
                    PayeeMerchantId="no establecido",
                    PayerEmail="no establecido",   
                    Description = "Pedido pagado",
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };
                await _payment.AgregarDetallePagoAsync(paymentDetail);
            }

            
            var capturePayment = new PayPalPaymentCapture
            {
                PaymentId = orderId,      
                CaptureId = captureId,
                PedidoId = pedido.Id,
                Amount = total,
                Currency = currency,
                Status = "COMPLETED",
                ProtectionEligibility="no",
                TransactionFeeAmount=0,
                TransactionFeeCurrency="no",
                ReceivableAmount=0,
                ReceivableCurrency="no",
                ExchangeRate=0,
                FinalCapture=false,
                DisputeCategories="no",
                CreateTime = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow
            };

            await _payment.AgregarCaptureAsync(capturePayment);

            // 5. Actualizar pedido 
            pedido.Total = total;
            pedido.Currency = currency;
            pedido.EstadoPedido = EstadoPedido.Pagado.ToString();

            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            return OperationResult<Pedido>.Ok("", pedido);
        }
        public async Task ProcesarRembolsoAsync(int pedidoId, string status, string refundId)
        {

            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);
            
            if (pedido == null)
                throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

            pedido.EstadoPedido = status;
            pedido.RefundId = refundId;
            foreach (var detalle in pedido.DetallePedidos)
            {
                detalle.Rembolsado = true;
            }
            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            var usuarioActual = _currentUserAccesor.GetCurrentUserId();

            // Crear o actualizar registro de reembolso
            var obtenerRembolso = await _paypalRepository.ObtenRembolsoAsync(pedido.NumeroPedido);

            if (obtenerRembolso == null)
            {
                var rembolso = new Rembolso
                {

                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                    EmailCliente = pedido.IdUsuarioNavigation?.Email,
                    FechaRembolso = DateTime.UtcNow,
                    MotivoRembolso = "Rembolso solicitado por el usuario",
                    EstadoRembolso = "REMBOLSO APROVADO",
                    ReembolsoCompletado = true,
                    UsuarioId = usuarioActual,
                    PedidoId = pedido.Id,
                    RefundIdPayPal=refundId,
                    MontoRembolsado=pedido.Total,
                    Currency=pedido.Currency,
                 
                };
                await _paypalRepository.AgregarRembolsoAsync(rembolso);
            }
            else
            {
                obtenerRembolso.EstadoRembolso = "REMBOLSO APROVADO";
                obtenerRembolso.ReembolsoCompletado = true;
               
                obtenerRembolso.FechaRembolso = DateTime.UtcNow;
                

                await _paypalRepository.ActualizarRembolsoAsync(obtenerRembolso);
            }


            _logger.LogInformation($"Estado del pedido {pedidoId} actualizado a {status}");


        }
        public async Task RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string motivo, decimal montoRembolsado, string currency, string refundId)
        {

            // Obtener el pedido con los datos relacionados
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

            if (pedido == null)
                throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

            // Obtener el detalle específico por ID
            var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
            if (detalleReembolsado == null)
                throw new ArgumentException($"Detalle con ID {detalleId} no encontrado.");

            // Evitar reembolsos duplicados
            if (detalleReembolsado.Rembolsado ?? false)
                throw new InvalidOperationException($"El detalle con ID {detalleId} ya ha sido reembolsado.");

            var usuarioActual = _currentUserAccesor.GetCurrentUserId();

            // Crear registro de reembolso
            var rembolso = new Rembolso
            {
                PedidoId = pedido.Id,
                NumeroPedido = pedido.NumeroPedido,
                NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                EmailCliente = pedido.IdUsuarioNavigation?.Email,
                FechaRembolso = DateTime.UtcNow,
                MotivoRembolso = motivo,
                EstadoRembolso = "REMBOLSO PARACIAL APROVADO",
                ReembolsoCompletado = true,
                UsuarioId = usuarioActual,
                MontoRembolsado=montoRembolsado,
                Currency=currency,
                RefundIdPayPal=refundId
               
            };

            await _paypalRepository.AgregarRembolsoAsync(rembolso);

            // Marcar el detalle correcto como reembolsado
            detalleReembolsado.Rembolsado = true;
            await _pedidoRepository.ActualizarDetallePedidoAsync(detalleReembolsado);

            _logger.LogInformation($"Reembolso registrado para pedido {pedidoId}, detalle {detalleId}.");

        }
        public async Task AddInfoTrackingOrder(int pedidoId, string tracking, string url, string carrier)
        {

            var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null)
                throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");
            pedido.EstadoPedido = EstadoPedido.Enviado.ToString();
            pedido.TrackingNumber = tracking;
            pedido.UrlTracking = url;
            pedido.Transportista = carrier;
            await _pedidoRepository.ActualizarPedidoAsync(pedido);


        }

    }
}
