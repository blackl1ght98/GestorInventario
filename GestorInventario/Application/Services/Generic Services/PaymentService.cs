using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs.Carrito;
using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;

using Newtonsoft.Json;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class PaymentService: IPaymentService
    {
       
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IUnitOfWork _unitOfWork;    
        private readonly ILogger<PaymentService> _logger;
        private readonly IPedidoManagementService _pedidoService;
        private readonly IConfiguration _configuration;
        private readonly IPaypalOrderService _paypalOrder;

        public PaymentService( ICurrentUserAccessor currentUserAccessor, ILogger<PaymentService> logger, IUnitOfWork unit,
        IPedidoManagementService pedidoService, IConfiguration configuration, IPaypalOrderService paypalOrder)
        {
            
            _currentUserAccessor = currentUserAccessor;
            _unitOfWork= unit;
            _logger = logger;
            _pedidoService = pedidoService;
            _configuration = configuration;
            _paypalOrder = paypalOrder;
         
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
    }
}
