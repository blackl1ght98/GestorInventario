using Aspose.Pdf.Operators;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using PayPal.Api;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CarritoRepository> _logger;    
        public CarritoRepository(GestorInventarioContext context, IUnitOfWork unit, IHttpContextAccessor contextAccessor, IConfiguration configuration, ILogger<CarritoRepository> logger)
        {
            _context = context;
            _unitOfWork = unit;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _logger = logger;
        }
      
        public async Task<Carrito> ObtenerCarritoUsuario(int userId)=>await _context.Carritos.FirstOrDefaultAsync(x => x.UsuarioId == userId);
        public async Task<List<ItemsDelCarrito>> ObtenerItemsDelCarritoUsuario(int userIdcarrito)=>await _context.ItemsDelCarritos.Where(i => i.CarritoId == userIdcarrito).ToListAsync();
        public async Task<ItemsDelCarrito> ItemsDelCarrito(int Id)=>await _context.ItemsDelCarritos.FirstOrDefaultAsync(x => x.Id == Id);      
        public async Task<(bool, string, string)> Pagar(string moneda, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var carrito = await ObtenerCarritoUsuario(userId);
                if (carrito == null)
                {
                    return (false, "El carrito está vacío o no se pudo obtener", null);
                }

                var itemsDelCarrito = await ObtenerItemsDelCarritoUsuario(carrito.Id);
                if (itemsDelCarrito == null || !itemsDelCarrito.Any())
                {
                    return (false, "No se pudieron obtener los items del carrito del usuario", null);
                }

                var pedido = new Pedido
                {
                    NumeroPedido = GenerarNumeroPedido(),
                    FechaPedido = DateTime.Now,
                    EstadoPedido = "En Proceso",
                    IdUsuario = userId
                };
                await _context.AddEntityAsync(pedido);

                foreach (var item in itemsDelCarrito)
                {
                    var detallePedido = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad ?? 0

                    };
                   await _context.AddEntityAsync(detallePedido);
                }

                var historialPedido = new HistorialPedido
                {
                    IdUsuario = pedido.IdUsuario,
                    Fecha = DateTime.Now,
                    Accion = "Nuevo pedido",
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
              await _context.AddEntityAsync(historialPedido);

                foreach (var item in itemsDelCarrito)
                {
                    var detalleHistorialPedido = new DetalleHistorialPedido
                    {
                        HistorialPedidoId = historialPedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad ?? 0
                    };
                  await  _context.AddEntityAsync(detalleHistorialPedido);
                }
                await _context.DeleteRangeEntityAsync(itemsDelCarrito);
                moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
                var items = new List<Item>();
                decimal totalAmount = 0;
                foreach (var item in itemsDelCarrito)
                {
                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null)
                    {
                        return (false, $"Producto con ID {item.ProductoId} no encontrado", null);
                    }
                    var paypalItem = new Item
                    {
                        name = producto.NombreProducto,
                        currency = moneda,
                        price = producto.Precio.ToString("0.00"),
                        quantity = item.Cantidad.ToString(),
                        sku = "producto"
                    };
                    items.Add(paypalItem);
                    totalAmount += Convert.ToDecimal(producto.Precio) * Convert.ToDecimal(item.Cantidad ?? 0);
                }
                var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
                string returnUrl = isDocker
                    ? _configuration["Paypal:returnUrlConDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlConDocker")
                    : _configuration["Paypal:returnUrlSinDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlSinDocker");
                string cancelUrl = "https://localhost:7056/Payment/Cancel";
                var createdPayment = await _unitOfWork.PaypalService.CreateOrderAsync(items, totalAmount, returnUrl, cancelUrl, moneda);
                string approvalUrl = createdPayment.links?.FirstOrDefault(x => x.rel.ToLower() == "approval_url")?.href;
                if (!string.IsNullOrEmpty(approvalUrl))
                {
                    await transaction.CommitAsync();
                    return (true, "Redirigiendo a PayPal para completar el pago", approvalUrl);
                }

                return (false, "Fallo al iniciar el pago con PayPal", null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al realizar el pago");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde",null);
            }
           
        }
        private string GenerarNumeroPedido()
        {
            var length = 10;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public IQueryable<ItemsDelCarrito> ObtenerItems(int id)=>_context.ItemsDelCarritos.Include(i => i.Producto).Include(i => i.Producto.IdProveedorNavigation).Where(i => i.CarritoId == id);
        public async Task<List<Monedum>> ObtenerMoneda()=>await _context.Moneda.ToListAsync();    
        public async Task<(bool, string)> Incremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var carrito = await ItemsDelCarrito(id);
                if (carrito != null)
                {
                    carrito.Cantidad++;
                    await _context.UpdateEntityAsync(carrito);
                    var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == carrito.ProductoId);
                    if (producto != null && producto.Cantidad > 0)
                    {
                        // Decrementa la cantidad del producto en la tabla de productos
                        producto.Cantidad--;
                        await _context.UpdateEntityAsync(producto);
                    }
                }
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al realizar el incremento");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }
           
        }
        public async Task<(bool, string)> Decremento(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var carrito = await ItemsDelCarrito(id);

                if (carrito != null)
                {
                    // Decrementa la cantidad del producto en el carrito
                    carrito.Cantidad--;
                    var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == carrito.ProductoId);
                    if (producto != null)
                    {
                        // Incrementa la cantidad del producto en la tabla de productos
                        producto.Cantidad++;
                        //_context.Productos.Update(producto);
                      await  _context.UpdateEntityAsync(producto);
                    }
                   await _context.UpdateEntityAsync(carrito);

                }
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al realizar el decremento");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }
            
        }
        public async Task<(bool, string)> EliminarProductoCarrito(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var itemsCarrito = await _context.ItemsDelCarritos.FirstOrDefaultAsync(x => x.Id == id);
                if (itemsCarrito == null)
                {
                    return (false, "No se puede eliminar porque no hay productos");
                }
                await _context.DeleteEntityAsync(itemsCarrito);
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == itemsCarrito.ProductoId);
                if (producto != null)
                {
                    // Incrementa la cantidad del producto en la tabla de productos
                    producto.Cantidad++;
                    //_context.Productos.Update(producto);
                   await _context.UpdateEntityAsync(producto);
                }
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al realizar la eliminacion del producto del carrito");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }
           
        }
    }
}
