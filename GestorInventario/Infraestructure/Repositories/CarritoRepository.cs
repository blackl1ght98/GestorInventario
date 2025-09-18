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
        public async Task<(Pedido?,string)> ObtenerCarritoUsuario(int userId)
        {
            var carrito = await _context.Pedidos
                .FirstOrDefaultAsync(x => x.IdUsuario == userId && x.EsCarrito);
            return carrito is null ? (null,"No se puede obtener el carrito del usuario"): (carrito,"Carrito obtenido con exito");
        }

        // Obtener ítems del carrito (DetallePedido para un Pedido con EsCarrito = 1)
        public async Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId)
        {
            return await _context.DetallePedidos
                .Where(i => i.PedidoId == pedidoId)
                .ToListAsync();
        }

        // Obtener un ítem específico del carrito por ID
        public async Task<(DetallePedido?,string)> ItemsDelCarrito(int id)
        {
            var itemsCarrito = await _context.DetallePedidos
                .FirstOrDefaultAsync(x => x.Id == id);
            return itemsCarrito is null ? (null, "No hay productos en el carrito") : (itemsCarrito, "Productos obtenidos");
        }

        // Obtener ítems con datos relacionados (producto y proveedor)
        public IQueryable<DetallePedido> ObtenerItemsConDetalles(int pedidoId)
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
        public async Task<Pedido?> CrearCarritoUsuario(int userId)
        {
           
            try
            {
                var (carrito,mensaje) = await ObtenerCarritoUsuario(userId);
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
                   
                }

                return carrito;
            }
            catch (Exception ex) {
                _logger.LogError(ex,"Ocurrio un error inesperado");
                return null;
            }
           
        }


        public async Task<(bool, string, string)> PagarV2(string moneda, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Recolectar información del usuario
                var (infoUsuario,mensaje) = await ValidarUsuarioYObtenerInfo();

                //Validar y obtener items del carrito
                var (success, message, carrito, itemsDelCarrito) = await ValidarCarritoYObtenerItems(userId);
                if (!success)
                {
                    return (false, message, "");
                }

                // Convertir el carrito en un pedido
                await ConvertirCarritoAPedido(carrito);

                // Calcular el total para PayPal
                moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
                var checkout = await PrepararCheckoutParaPagoPayPal(itemsDelCarrito, moneda, infoUsuario);
                // Iniciar pago con PayPal
                var (paypalSuccess, paypalMessage, approvalUrl) = await ProcesarPagoPayPal(checkout);
                if (!paypalSuccess)
                {
                    await transaction.RollbackAsync();
                    return (false, paypalMessage, "");
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
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.", "");
            }
        }
        private async Task<(InfoUsuario?,string)> ValidarUsuarioYObtenerInfo()
        {
            var usuarioId = _utilityClass.ObtenerUsuarioIdActual();
            var (usuarioActual,mensaje) = await _admin.ObtenerPorId(usuarioId);
            if(usuarioActual == null)
            {
                return (null, "El usuario no existe");
            }
            var infoUsuario = new InfoUsuario
            {
                NombreCompletoUsuario = usuarioActual.NombreCompleto ?? "Nombre no facilitado",
                Telefono = usuarioActual.Telefono ?? "Telefono no facilitado",
                CodigoPostal = usuarioActual.CodigoPostal ?? "Codigo Postal no facilitado",
                Ciudad = usuarioActual.Ciudad ?? "Ciudad no facilitado",
                Line1 = usuarioActual.Direccion.Split(",")[0].Trim(),
                Line2 = usuarioActual.Direccion.Split(",").Length > 1 ? usuarioActual.Direccion.Split(",")[1].Trim() : ""
            };
           
            return (infoUsuario,"Informacion obtenida");
        }
        private async Task<(bool Success, string Message, Pedido? Carrito, List<DetallePedido> Items)> ValidarCarritoYObtenerItems(int userId)
        {
            var (carrito, mensaje) = await ObtenerCarritoUsuario(userId);
            if (carrito == null)
            {
                return (false, "No se encontró un carrito para el usuario.", null, new List<DetallePedido>());
            }

            var itemsDelCarrito = await ObtenerItemsDelCarritoUsuario(carrito.Id);
            if (!itemsDelCarrito.Any())
            {
                return (false, "El carrito está vacío.", carrito, itemsDelCarrito);
            }

            return (true, string.Empty, carrito, itemsDelCarrito);
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
                
                _logger.LogError(ex,"Ocurrio un error inesperado");
            
            }
           
        }
       
        private async Task<Checkout> PrepararCheckoutParaPagoPayPal(List<DetallePedido> itemsDelCarrito, string moneda, InfoUsuario infoUsuario)
        {
            var items = new List<ItemModel>();
            decimal totalAmount = 0;
           
                foreach (var item in itemsDelCarrito)
                {
               
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto != null)
                    {
                   
                        var paypalItem = new ItemModel
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

            return new Checkout
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
        private async Task<(bool, string, string)> ProcesarPagoPayPal(Checkout checkout)
        {
            var createdPaymentJson = await _paypalService.CreateOrderWithPaypalAsync(checkout);
            var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(createdPaymentJson);
            var approvalUrl = createdPayment?.Links?.FirstOrDefault(x => x.Rel == "payer-action")?.Href;
            if (!string.IsNullOrEmpty(approvalUrl))
            {
                return (true, "Redirigiendo a PayPal para completar el pago", approvalUrl);
            }
            return (false, "Fallo al iniciar el pago con PayPal", "");
        }


        private async Task RegistrarHistorialPedido(Pedido pedido, List<DetallePedido> itemsDelCarrito)
        {
            string ipString;
            if (_contextAccessor.HttpContext == null)
            {
                ipString = string.Empty; 
            }
            else
            {
                var ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress;
                ipString = ip != null
                    ? ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                        ? ip.MapToIPv4().ToString()
                        : ip.ToString()
                    : string.Empty;
            }


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
                        await _context.DeleteEntityAsync(carritoActivo);
                        _logger.LogInformation($"Carrito vacío eliminado para el usuario {userId}, ID: {carritoActivo.Id}");
                    }
                }
                await _context.SaveChangesAsync();
               

            } catch (Exception ex) {
                _logger.LogError(ex,"Ocurrio un error inesperado");
               
           }
           
        }


        public async Task<(bool, string)> Incremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var (detalle,mensaje) = await ItemsDelCarrito(id);
                if (detalle == null)
                {
                    return (false, mensaje);
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
                return (true, "");
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
                var (detalle,mensaje) = await ItemsDelCarrito(id);
                if (detalle == null)
                {
                    return (false, mensaje);
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
                return (true, "");
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
                return (true, "");
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