using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs.Carrito;
using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
using GestorInventario.ViewModels.Paypal;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class PaymentService: IPaymentService
    {
       
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IUnitOfWork _unitOfWork;    
        private readonly ILogger<PaymentService> _logger;      
        private readonly IConfiguration _configuration;
        private readonly IPaypalOrderService _paypalOrder;
        private readonly IConversionUtils _conversion;
        private readonly IEmailService _emailService;
        private readonly IPaypalRepository _paypalRepository;
        private readonly IPaymentRepository _paymentrepository;
        private readonly IPedidoRepository _pedidoRepository;
        public PaymentService( ICurrentUserAccessor currentUserAccessor, ILogger<PaymentService> logger, IUnitOfWork unit, IPaymentRepository payment,
        IConfiguration configuration, IPaypalOrderService paypalOrder, IConversionUtils conversion, IEmailService email, IPaypalRepository paypal, IPedidoRepository pedido)
        {
            
            _currentUserAccessor = currentUserAccessor;
            _unitOfWork= unit;
            _logger = logger;      
            _configuration = configuration;
            _paypalOrder = paypalOrder;
            _conversion = conversion;
            _emailService = email;
            _paypalRepository= paypal;
            _paymentrepository = payment;
            _pedidoRepository = pedido;
         
        }

        public async Task<OperationResult<string>> Pagar(string moneda, int userId)
        {
           
                // Recolectar información del usuario
                var result = await ValidarUsuarioYObtenerInfo();
                if (!result.Success)
                    return OperationResult<string>.Fail(result.Message);

                var infoUsuario = result.Data;

                // Validar carrito
                var resultado = await ValidarCarritoYObtenerItems(userId);
                if (!resultado.Success)
                    return OperationResult<string>.Fail(resultado.Message);

                var carrito = resultado.Data.Carrito;
                var itemsDelCarrito = resultado.Data.Items;

                // Convertir carrito a pedido
                await ConvertirCarritoAPedido(carrito);

                // Preparar y procesar pago con PayPal
                moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
                var checkout = await PrepararCheckoutParaPagoPayPal(itemsDelCarrito, moneda, infoUsuario);

                var approvalUrl = await ProcesarPagoPayPal(checkout);

                if (!approvalUrl.Success)
                {

                    return OperationResult<string>.Fail(approvalUrl.Message);
                }

                await EliminarCarritosVaciosUsuario(userId);
                return OperationResult<string>.Ok(
                    "Redirigiendo a PayPal para completar el pago",
                    approvalUrl.Data);
        
        }
        private async Task<OperationResult<InfoUsuarioDto>> ValidarUsuarioYObtenerInfo()
        {
            var usuarioId = _currentUserAccessor.GetCurrentUserId();
            var usuarioActual = await _unitOfWork.UserRepository.ObtenerUsuarioPorId(usuarioId);
            if (usuarioActual == null)
            {
                return OperationResult<InfoUsuarioDto>.Fail("El usuario no existe");
            }
            var infoUsuario = new InfoUsuarioDto
            {
                NombreCompletoUsuario = usuarioActual.Data.NombreCompleto ?? "Nombre no facilitado",
                Telefono = usuarioActual.Data.Telefono ?? "Telefono no facilitado",
                CodigoPostal = usuarioActual.Data.CodigoPostal ?? "Codigo Postal no facilitado",
                Ciudad = usuarioActual.Data.Ciudad ?? "Ciudad no facilitado",
                Line1 = usuarioActual.Data.Direccion.Split(",")[0].Trim(),
                Line2 = usuarioActual.Data.Direccion.Split(",").Length > 1 ? usuarioActual.Data.Direccion.Split(",")[1].Trim() : ""
            };

            return OperationResult<InfoUsuarioDto>.Ok("Validacion exitosa", infoUsuario);
        }
        private async Task<OperationResult<CarritoConItemsDto>> ValidarCarritoYObtenerItems(int userId)
        {
            var result = await _unitOfWork.CarritoRepository.ObtenerCarritoUsuario(userId);
            var carrito = result.Data;
            if (carrito == null)
            {
                return OperationResult<CarritoConItemsDto>.Fail("No se encontró un carrito para el usuario.");
            }

            var itemsDelCarrito = await _unitOfWork.CarritoRepository.ObtenerItemsDelCarritoUsuario(carrito.Id);
            if (!itemsDelCarrito.Data.Any())
            {
                return OperationResult<CarritoConItemsDto>.Fail("El carrito está vacío.");
            }

            var resultado = new CarritoConItemsDto
            {
                Carrito = carrito,
                Items = itemsDelCarrito.Data
            };

            return OperationResult<CarritoConItemsDto>.Ok("Validacion exitosa", resultado);
        }

        private async Task ConvertirCarritoAPedido(Pedido carrito)
        {

            try
            {
                carrito.EsCarrito = false;
                carrito.NumeroPedido = GenerarNumPedido.GenerarNumeroPedido();
                carrito.FechaPedido = DateTime.Now;
                carrito.EstadoPedido = EstadoPedido.En_Proceso.ToString();
                await _unitOfWork.PedidoRepository.ActualizarPedidoAsync(carrito);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Ocurrio un error inesperado");

            }

        }

        private async Task<CheckoutDto> PrepararCheckoutParaPagoPayPal(List<DetallePedido> itemsDelCarrito, string moneda, InfoUsuarioDto infoUsuario)
        {
            var items = new List<ItemModelDto>();
            decimal totalAmount = 0;

            foreach (var item in itemsDelCarrito)
            {

                var producto = await _unitOfWork.ProductoRepository.ObtenerProductoPorIdAsync(item.ProductoId.Value);
                if (producto != null)
                {

                    var paypalItem = new ItemModelDto
                    {
                        Name = producto.NombreProducto,
                        Currency = moneda,
                        Price = producto.Precio,

                        Quantity = item.Cantidad.Value.ToString(),
                        Sku = producto.Descripcion
                    };
                    items.Add(paypalItem);
                    totalAmount += Convert.ToDecimal(producto.Precio) * Convert.ToDecimal(item.Cantidad ?? 0);
                }


            }

            string returnUrl = ObtenerReturnUrl();
            string cancelUrl = "https://localhost:7056/Payment/Cancel";

            return new CheckoutDto
            {
                TotalAmount = totalAmount,
                Currency = moneda,
                Items = items,
                NombreCompleto = infoUsuario.NombreCompletoUsuario,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Telefono = infoUsuario.Telefono,
                Ciudad = infoUsuario.Ciudad,
                CodigoPostal = infoUsuario.CodigoPostal,
                Line1 = infoUsuario.Line1,
                Line2 = infoUsuario.Line2
            };
        }
        private string ObtenerReturnUrl()
        {
            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
            var configKey = isDocker ? "Paypal:returnUrlConDocker" : "Paypal:returnUrlSinDocker";
            var envVarKey = isDocker ? "Paypal_returnUrlConDocker" : "Paypal_returnUrlSinDocker";

            var returnUrl = _configuration[configKey] ?? Environment.GetEnvironmentVariable(envVarKey);
            return returnUrl ?? throw new InvalidOperationException($"La URL de retorno no está configurada. Verifique la clave '{configKey}' o la variable de entorno '{envVarKey}'.");
        }
        private async Task<OperationResult<string>> ProcesarPagoPayPal(CheckoutDto checkout)
        {
            var createdPaymentJson = await _paypalOrder.CreateOrderWithPaypalAsync(checkout);
            var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(createdPaymentJson);
            var approvalUrl = createdPayment?.Links?.FirstOrDefault(x => x.Rel == "payer-action")?.Href;
            if (!string.IsNullOrEmpty(approvalUrl))
            {
                return OperationResult<string>.Ok("Redirigiendo a PayPal para completar el pago", approvalUrl);

            }
            return OperationResult<string>.Fail("Error al procesar el pago con paypal");
        }



        private async Task EliminarCarritosVaciosUsuario(int userId)
        {

            var carritosActivos = await _unitOfWork.CarritoRepository.ObtenerCarritosActivosAsync(userId);
            foreach (var carritoActivo in carritosActivos)
            {
                if (!carritoActivo.DetallePedidos.Any())
                {
                    await _unitOfWork.PedidoRepository.EliminarPedidoAsync(carritoActivo);
                    _logger.LogInformation($"Carrito vacío eliminado para el usuario {userId}, ID: {carritoActivo.Id}");
                }
            }
            
        }
        public OperationResult<PayPalPaymentDetail> ProcesarDetallesPagoAsync(OrderDetailsResponse detallespago)
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
        public async Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundFormViewModel form, Pedido obtenerNumeroPedido, string emailCliente)
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

            await _paypalRepository.AgregarRembolsoAsync(rembolso);


            var emailRembolso = new EmailRembolsoDto
            {
                NumeroPedido = rembolso.NumeroPedido,
                NombreCliente = rembolso.NombreCliente,
                EmailCliente = rembolso.EmailCliente,
                FechaRembolso = rembolso.FechaRembolso,
                MotivoRembolso = rembolso.MotivoRembolso,
                Productos = paypalItems
            };
            await _emailService.EnviarEmailSolicitudRembolso(emailRembolso);

            if (!paypalItems.Any())
            {
                _logger.LogError($"No se encontraron ítems en PurchaseUnits para el reembolso del pedido {form.NumeroPedido}.");
                return OperationResult<PayPalPaymentItem>.Fail("No se puede procesar el rembolso sin items asociados");
            }
            return OperationResult<PayPalPaymentItem>.Ok("", paypalItems.First());

        }
        public async Task LimpiarPedidoCorruptoUsuarioAsync(int userId)
        {
            // Buscar SOLO UN pedido pendiente/corrupto del usuario
            var pedidoCorrupto = await _paymentrepository.BuscarPedidoCorrupto(userId);

            if (pedidoCorrupto != null)
            {
                // Opcional: verificar que efectivamente es un carrito/checkout abandonado
                if (pedidoCorrupto.EsCarrito == false)
                {
                    await _pedidoRepository.EliminarPedidoAsync(pedidoCorrupto);
                   

                    _logger.LogInformation(
                        "Limpieza automática: pedido corrupto eliminado para usuario {UserId}. ID: {PedidoId}, Fecha: {FechaPedido}",
                        userId, pedidoCorrupto.Id, pedidoCorrupto.FechaPedido);
                }
            }
        }
    }
}
