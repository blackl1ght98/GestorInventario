using GestorInventario.Application.DTOs.Carrito;
using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Order;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels.Paypal;
using Newtonsoft.Json;

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
       
       
        public PaymentService( ICurrentUserAccessor currentUserAccessor, ILogger<PaymentService> logger, IUnitOfWork unit,
        IConfiguration configuration, IPaypalOrderService paypalOrder, IConversionUtils conversion, IEmailService email, IPaypalRepository paypal)
        {
            
            _currentUserAccessor = currentUserAccessor;
            _unitOfWork= unit;
            _logger = logger;      
            _configuration = configuration;
            _paypalOrder = paypalOrder;
            _conversion = conversion;
            _emailService = email;
            _paypalRepository= paypal;
          
          
         
        }

        public async Task<OperationResult<string>> Pagar(string moneda, int userId)
        {
            // 1. Info usuario
            var result = await ValidarUsuarioYObtenerInfo();
            if (!result.Success)
                return OperationResult<string>.Fail(result.Message);

            var infoUsuario = result.Data;

            // 2. Validar carrito
            var resultado = await ValidarCarritoYObtenerItems(userId);
            if (!resultado.Success)
                return OperationResult<string>.Fail(resultado.Message);

            var carrito = resultado.Data.Carrito;
            var itemsDelCarrito = resultado.Data.Items;

            // 3. Preparar checkout (calcula subtotal, iva, total)
            moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
            var checkout = await PrepararCheckoutParaPagoPayPal(itemsDelCarrito, moneda, infoUsuario);

            // 4. ✅ Convertir carrito a pedido PERSISTIENDO los totales calculados
            await ConvertirCarritoAPedido(carrito, checkout);

            // 5. Procesar pago con PayPal
            var approvalUrl = await ProcesarPagoPayPal(checkout);

            if (!approvalUrl.Success)
                return OperationResult<string>.Fail(approvalUrl.Message);

            // 6. Limpiar
            await EliminarCarritosVaciosUsuario(userId);

            return OperationResult<string>.Ok(
                "Redirigiendo a PayPal para completar el pago",
                approvalUrl.Data);
        }
        public async Task<OperationResult<string>> ReintentarPago(int pedidoId)
        {
     
            var pedido = await _unitOfWork.PedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null)
                return OperationResult<string>.Fail("Pedido no encontrado.");

            if (pedido.EstadoPedido != EstadoPedido.Pendiente.ToString())
                return OperationResult<string>.Fail("El pedido no está pendiente de pago.");

            
            var itemsPedido = await _unitOfWork.PedidoRepository.ObtenerDetallesPedidoAsync(pedidoId);
            if (!itemsPedido.Any())
                return OperationResult<string>.Fail("El pedido no tiene items.");

         
            var result = await ValidarUsuarioYObtenerInfo();
            if (!result.Success)
                return OperationResult<string>.Fail(result.Message);

         
            var checkout = await PrepararCheckoutParaPagoPayPal(
                itemsPedido,
                pedido.Currency ?? "EUR",
                result.Data);

         
            checkout.Subtotal = pedido.Subtotal;
            checkout.Iva = pedido.Iva;
            checkout.TotalAmount = pedido.Total;
           

          
            var approvalUrl = await ProcesarPagoPayPal(checkout);
            if (!approvalUrl.Success)
                return OperationResult<string>.Fail(approvalUrl.Message);

           

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
                NombreCompletoUsuario = usuarioActual.NombreCompleto ?? "Nombre no facilitado",
                Telefono = usuarioActual.Telefono ?? "Telefono no facilitado",
                CodigoPostal = usuarioActual.CodigoPostal ?? "Codigo Postal no facilitado",
                Ciudad = usuarioActual.Ciudad ?? "Ciudad no facilitado",
                Line1 = usuarioActual.Direccion.Split(",")[0].Trim(),
                Line2 = usuarioActual.Direccion.Split(",").Length > 1 ? usuarioActual.Direccion.Split(",")[1].Trim() : ""
            };

            return OperationResult<InfoUsuarioDto>.Ok("Validacion exitosa", infoUsuario);
        }
        private async Task<OperationResult<CarritoConItemsDto>> ValidarCarritoYObtenerItems(int userId)
        {
            var carrito = await _unitOfWork.CarritoRepository.ObtenerCarritoUsuario(userId);
          
            if (carrito == null)
            {
                return OperationResult<CarritoConItemsDto>.Fail("No se encontró un carrito para el usuario.");
            }

            var itemsDelCarrito = await _unitOfWork.CarritoRepository.ObtenerItemsDelCarritoUsuario(carrito.Id);
            if (!itemsDelCarrito.Any())
            {
                return OperationResult<CarritoConItemsDto>.Fail("El carrito está vacío.");
            }

            var resultado = new CarritoConItemsDto
            {
                Carrito = carrito,
                Items = itemsDelCarrito
            };

            return OperationResult<CarritoConItemsDto>.Ok("Validacion exitosa", resultado);
        }


        private async Task ConvertirCarritoAPedido(Pedido carrito, CheckoutDto checkout)
        {
            if (checkout == null)
                throw new ArgumentNullException(nameof(checkout));

            if (checkout.TotalAmount <= 0)
                throw new InvalidOperationException(
                    $"No se puede crear un pedido con total 0. Subtotal:{checkout.Subtotal}, Iva:{checkout.Iva}");

            if (string.IsNullOrWhiteSpace(checkout.Currency))
                throw new InvalidOperationException("La moneda es obligatoria para crear el pedido.");

            try
            {
                carrito.EsCarrito = false;
                carrito.NumeroPedido = GenerarNumPedido.GenerarNumeroPedido();
                carrito.FechaPedido = DateTime.UtcNow;
                carrito.EstadoPedido = EstadoPedido.Pendiente.ToString();

                carrito.Subtotal = checkout.Subtotal;
                carrito.Iva = checkout.Iva;
                carrito.Total = checkout.TotalAmount;
                carrito.Currency = checkout.Currency;

                _logger.LogInformation(
                    "Persistiendo pedido {PedidoId} -> Subtotal:{Subtotal} Iva:{Iva} Total:{Total} {Currency}",
                    carrito.Id, checkout.Subtotal, checkout.Iva, checkout.TotalAmount, checkout.Currency);

                await _unitOfWork.PedidoRepository.ActualizarPedidoAsync(carrito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al convertir carrito a pedido ID {PedidoId}", carrito.Id);
                throw;
            }
        }
        private async Task<CheckoutDto> PrepararCheckoutParaPagoPayPal(
         List<DetallePedido> itemsDelCarrito,
         string moneda,
         InfoUsuarioDto infoUsuario
         )
        {
            var items = new List<ItemModelDto>();
            var lineasParaCalculo = new List<(decimal precio, int cantidad)>();

            foreach (var item in itemsDelCarrito)
            {
                var producto = await _unitOfWork.ProductoRepository
                    .ObtenerProductoPorIdAsync(item.ProductoId.Value);

                if (producto == null) continue;

                var cantidad = item.Cantidad ?? 0;
                var precio = Convert.ToDecimal(producto.Precio);

                items.Add(new ItemModelDto
                {
                    Name = producto.NombreProducto,
                    Currency = moneda,
                    Price = producto.Precio,
                    Quantity = cantidad.ToString(),
                    Sku = producto.Descripcion
                });

                lineasParaCalculo.Add((precio, cantidad));
            }

            var (subtotal, iva, total) = CalculadoraFiscal.CalcularTotales(lineasParaCalculo);

            string returnUrl = ObtenerReturnUrl();
            string cancelUrl = "https://localhost:7056/Payment/Cancel";

            return new CheckoutDto
            {
                TotalAmount = total,        
                Subtotal = subtotal,        
                Iva = iva,                  
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

            return _configuration[configKey]
                ?? Environment.GetEnvironmentVariable(envVarKey)
                ?? throw new InvalidOperationException("URL de retorno no configurada.");
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
                    await _unitOfWork.PedidoRepository.EliminarCarritoAsync(carritoActivo);
                    _logger.LogInformation($"Carrito vacío eliminado para el usuario {userId}, ID: {carritoActivo.Id}");
                }
            }
            
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


            var emailRembolso = new EmailReembolsoAprobadoDto
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
       

    }
}
