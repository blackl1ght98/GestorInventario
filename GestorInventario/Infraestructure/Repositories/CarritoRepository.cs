using GestorInventario.Application.Classes;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IPaypalService _paypalService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CarritoRepository> _logger;
        private readonly IAdminRepository _admin;
        private readonly UtilityClass _utilityClass;
        public CarritoRepository(GestorInventarioContext context, IPaypalService service, IHttpContextAccessor contextAccessor,
            IConfiguration configuration, ILogger<CarritoRepository> logger, IAdminRepository admin, UtilityClass utility)
        {
            _context = context;
            _paypalService = service;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _logger = logger;
            _admin = admin;
            _utilityClass = utility;
        }

        // Obtener el carrito del usuario (Pedidos con EsCarrito = 1)
        public async Task<Pedido> ObtenerCarritoUsuario(int userId)
        {
            return await _context.Pedidos
                .FirstOrDefaultAsync(x => x.IdUsuario == userId && x.EsCarrito);
        }

        // Obtener ítems del carrito (DetallePedido para un Pedido con EsCarrito = 1)
        public async Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId)
        {
            return await _context.DetallePedidos
                .Where(i => i.PedidoId == pedidoId)
                .ToListAsync();
        }

        // Obtener un ítem específico del carrito por ID
        public async Task<DetallePedido> ItemsDelCarrito(int id)
        {
            return await _context.DetallePedidos
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // Obtener ítems con datos relacionados (producto y proveedor)
        public IQueryable<DetallePedido> ObtenerItems(int pedidoId)
        {
            return _context.DetallePedidos
                .Include(i => i.Producto)
                .Include(i => i.Producto.IdProveedorNavigation)
                .Where(i => i.PedidoId == pedidoId);
        }

        // Obtener monedas disponibles
        public async Task<List<Monedum>> ObtenerMoneda()
        {
            return await _context.Moneda.ToListAsync();
        }

        private string GenerarNumeroPedido()
        {
            var length = 10;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Método para crear un carrito si no existe
        public async Task<Pedido> CrearCarritoUsuario(int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var carrito = await ObtenerCarritoUsuario(userId);
                if (carrito == null)
                {
                    carrito = new Pedido
                    {
                        IdUsuario = userId,
                        NumeroPedido = GenerarNumeroPedido(),
                        FechaPedido = DateTime.Now,
                        EstadoPedido = "Carrito",
                        EsCarrito = true
                    };
                    await _context.AddEntityAsync(carrito);
                    await transaction.CommitAsync();
                }

                return carrito;
            }
            catch (Exception ex) {
                _logger.LogError("Ocurrio un error inesperado", ex);
                return null;
            }
           
        }


        public async Task<(bool, string, string)> PagarV2(string moneda, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Recolectar información del usuario
                var infoUsuario = await ValidarUsuarioYObtenerInfo();
                
                //Validar y obtener items del carrito
                var (carrito, itemsDelCarrito)= await ValidarCarritoYObtenerItems(userId);
                if (carrito == null)
                {
                    return (false, "No se encontró un carrito para el usuario.", null);
                }
                if (!itemsDelCarrito.Any())
                {
                    return (false, "El carrito está vacío.", null);
                }
                // Convertir el carrito en un pedido
                await ConvertirCarritoAPedido(carrito);

                // Calcular el total para PayPal
                moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
                var checkout = await PrepararCheckoutParaPagoPayPal(itemsDelCarrito, moneda, infoUsuario);
                // Iniciar pago con PayPal
                var (success, message, approvalUrl) = await ProcesarPagoPayPal(checkout);
                if (!success)
                {
                    await transaction.RollbackAsync();
                    return (false, message, null);
                }
                await RegistrarHistorialPedido(carrito, itemsDelCarrito);
                await EliminarCarritosVaciosUsuario(userId);

                await transaction.CommitAsync();
                return (true, "Redirigiendo a PayPal para completar el pago", approvalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el pago");
                await transaction.RollbackAsync();
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.", null);
            }
        }
        private async Task<InfoUsuario> ValidarUsuarioYObtenerInfo()
        {
            var usuarioId = _utilityClass.ObtenerUsuarioIdActual();
            var usuarioActual = await _admin.ObtenerUsuarioId(usuarioId);

            // Recolectar información del usuario
            return new InfoUsuario
            {
                nombreCompletoUsuario = usuarioActual.NombreCompleto,
                telefono = usuarioActual.Telefono,
                codigoPostal = usuarioActual.CodigoPostal,
                ciudad = usuarioActual.Ciudad,
                line1 = usuarioActual.Direccion.Split(",")[0].Trim(),
                line2 = usuarioActual.Direccion.Split(",").Length > 1 ? usuarioActual.Direccion.Split(",")[1].Trim() : ""
            };
        }
        private async Task<(Pedido, List<DetallePedido>)> ValidarCarritoYObtenerItems(int userId)
        {
            // Obtener el carrito del usuario
            var carrito = await ObtenerCarritoUsuario(userId);
            if (carrito == null)
            {
                return (null, null);
            }
            var itemsDelCarrito = await ObtenerItemsDelCarritoUsuario(carrito.Id);
            return (carrito, itemsDelCarrito);
        }
        private async Task ConvertirCarritoAPedido(Pedido carrito)
        {
           
            try {
                carrito.EsCarrito = false;
                carrito.NumeroPedido = GenerarNumeroPedido();
                carrito.FechaPedido = DateTime.Now;
                carrito.EstadoPedido = "En Proceso";
                await _context.UpdateEntityAsync(carrito);
             
            } catch (Exception ex) {
                
                _logger.LogError("Ocurrio un error inesperado", ex);
            
            }
           
        }
       
        private async Task<Checkout> PrepararCheckoutParaPagoPayPal(List<DetallePedido> itemsDelCarrito, string moneda, InfoUsuario infoUsuario)
        {
            var items = new List<ItemModel>();
            decimal totalAmount = 0;

            foreach (var item in itemsDelCarrito)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                var paypalItem = new ItemModel
                {
                    name = producto.NombreProducto,
                    currency = moneda,
                    price = producto.Precio,
                    quantity = item.Cantidad.Value.ToString(),
                    sku = producto.Descripcion
                };
                items.Add(paypalItem);
                totalAmount += Convert.ToDecimal(producto.Precio) * Convert.ToDecimal(item.Cantidad ?? 0);
            }

            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
            string returnUrl = isDocker
                ? _configuration["Paypal:returnUrlConDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlConDocker")
                : _configuration["Paypal:returnUrlSinDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlSinDocker");
            string cancelUrl = "https://localhost:7056/Payment/Cancel";

            return new Checkout
            {
                totalAmount = totalAmount,
                currency = moneda,
                items = items,
                nombreCompleto = infoUsuario.nombreCompletoUsuario,
                returnUrl = returnUrl,
                cancelUrl = cancelUrl,
                telefono = infoUsuario.telefono,
                ciudad = infoUsuario.ciudad,
                codigoPostal = infoUsuario.codigoPostal,
                line1 = infoUsuario.line1,
                line2 = infoUsuario.line2
            };
        }
        private async Task<(bool, string, string)> ProcesarPagoPayPal(Checkout checkout)
        {
            var createdPaymentJson = await _paypalService.CreateOrderWithPaypalAsync(checkout);
            var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(createdPaymentJson);
            var approvalUrl = createdPayment?.links?.FirstOrDefault(x => x.rel == "payer-action")?.href;
            if (!string.IsNullOrEmpty(approvalUrl))
            {
                return (true, "Redirigiendo a PayPal para completar el pago", approvalUrl);
            }
            return (false, "Fallo al iniciar el pago con PayPal", null);
        }


        private async Task RegistrarHistorialPedido(Pedido pedido, List<DetallePedido> itemsDelCarrito)
        {
            var ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress;
            string ipString = (ip != null && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                ? ip.MapToIPv4().ToString()
                : ip?.ToString();

           
            try
            {
                // Crear el registro del historial del pedido
                var historialPedido = new HistorialPedido
                {
                    IdUsuario = pedido.IdUsuario,
                    Fecha = DateTime.UtcNow, 
                    Accion = "Nuevo pedido",
                    Ip = ipString
                };
                await _context.AddEntityAsync(historialPedido);

                // Agregar los detalles del historial
                foreach (var item in itemsDelCarrito)
                {
                    var detalleHistorialPedido = new DetalleHistorialPedido
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad ?? 0,
                        NumeroPedido = pedido.NumeroPedido,
                        FechaPedido = DateTime.UtcNow, 
                        EstadoPedido = pedido.EstadoPedido
                    };
                    await _context.AddEntityAsync(detalleHistorialPedido);
                }

               
                _logger.LogInformation($"Historial registrado para pedido {pedido.NumeroPedido}, ID {historialPedido.Id}");
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, $"Error al registrar historial para pedido {pedido.NumeroPedido}");
                throw;
            }
        }
        private async Task EliminarCarritosVaciosUsuario(int userId)
        {
          
            try {
                var carritosActivos = await _context.Pedidos
                   .Include(p => p.DetallePedidos)
                   .Where(p => p.IdUsuario == userId && p.EsCarrito)
                   .ToListAsync();
                foreach (var carritoActivo in carritosActivos)
                {
                    if (!carritoActivo.DetallePedidos.Any())
                    {
                        _context.Pedidos.Remove(carritoActivo);
                        _logger.LogInformation($"Carrito vacío eliminado para el usuario {userId}, ID: {carritoActivo.Id}");
                    }
                }
                await _context.SaveChangesAsync();
               

            } catch (Exception ex) {
                _logger.LogError("Ocurrio un error inesperado", ex);
               
           }
           
        }


        public async Task<(bool, string)> Incremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detalle = await ItemsDelCarrito(id);
                if (detalle == null)
                {
                    return (false, "El producto no está en el carrito.");
                }

                detalle.Cantidad++;
                await _context.UpdateEntityAsync(detalle);

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                if (producto == null || producto.Cantidad <= 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "No hay suficiente inventario para el producto seleccionado.");
                }

                producto.Cantidad--;
                await _context.UpdateEntityAsync(producto);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el incremento");
                await transaction.RollbackAsync();
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.");
            }
        }

        public async Task<(bool, string)> Decremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detalle = await ItemsDelCarrito(id);
                if (detalle == null)
                {
                    return (false, "El producto no está en el carrito.");
                }

                detalle.Cantidad--;
                if (detalle.Cantidad <= 0)
                {
                    await _context.DeleteEntityAsync(detalle);
                }
                else
                {
                    await _context.UpdateEntityAsync(detalle);
                }

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "El producto no existe.");
                }

                producto.Cantidad++;
                await _context.UpdateEntityAsync(producto);

                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el decremento");
                await transaction.RollbackAsync();
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.");
            }
        }

        public async Task<(bool, string)> EliminarProductoCarrito(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var detalle = await _context.DetallePedidos.FirstOrDefaultAsync(x => x.Id == id);
                if (detalle == null)
                {
                    return (false, "No se puede eliminar porque no hay productos.");
                }

                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                if (producto != null)
                {
                    producto.Cantidad += detalle.Cantidad ?? 0;
                    await _context.UpdateEntityAsync(producto);
                }

                await _context.DeleteEntityAsync(detalle);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar la eliminación del producto del carrito");
                await transaction.RollbackAsync();
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.");
            }
        }
    }
}