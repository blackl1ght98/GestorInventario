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
        public CarritoRepository(GestorInventarioContext context, IUnitOfWork unit, IHttpContextAccessor contextAccessor, IConfiguration configuration)
        {
            _context = context;
            _unitOfWork = unit;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
        }
        public async Task<Carrito> ObtenerCarrito(int userId)
        {
            return await _context.Carritos.FirstOrDefaultAsync(x => x.UsuarioId == userId);
        }
        public async Task<List<ItemsDelCarrito>> ObtenerItemsCarrito(int userIdcarrito)
        {
            return await _context.ItemsDelCarritos
                        .Include(i => i.Producto)

                         .Include(i => i.Producto.IdProveedorNavigation)

                        .Where(i => i.CarritoId == userIdcarrito)
                        .ToListAsync();
        }
        public async Task<List<ItemsDelCarrito>> ConvertirItemsAPedido(int userIdcarrito)
        {
            return await _context.ItemsDelCarritos
                        .Where(i => i.CarritoId == userIdcarrito)
                        .ToListAsync();
        }
        public async Task<ItemsDelCarrito> ItemsDelCarrito(int Id)
        {
            return await _context.ItemsDelCarritos.FirstOrDefaultAsync(x => x.Id == Id);
        }

        //Esto no esta como se suele poner Task<(bool, string) porque paypal necesita un valor que esta en uno de esos string que es la url de
        //aprobacion esta url ya contiene el paymentId, entonces porque 2 string y no solo 1, la respuesta es que uno de ellos es la respuesta de exito
        //o fracaso y el otro contiene la url de aprobacion
        //public async Task<(bool, string, string)> Pagar(string moneda, int userId)
        //{
        //    var carrito = await ObtenerCarrito(userId);
        //    if (carrito != null)
        //    {
        //        var itemsDelCarrito = await ConvertirItemsAPedido(carrito.Id);
        //        var pedido = new Pedido
        //        {
        //            NumeroPedido = GenerarNumeroPedido(),
        //            FechaPedido = DateTime.Now,
        //            EstadoPedido = "En Proceso",
        //            IdUsuario = userId
        //        };
        //        _context.AddEntity(pedido);
        //        // Este bucle se va a recorrer como items existan en el carrito
        //        foreach (var item in itemsDelCarrito)
        //        {
        //            var detallePedido = new DetallePedido
        //            {
        //                PedidoId = pedido.Id,
        //                ProductoId = item.ProductoId,
        //                Cantidad = item.Cantidad ?? 0
        //            };
        //            _context.AddEntity(detallePedido);
        //        }
        //        var historialPedido = new HistorialPedido()
        //        {
        //            IdUsuario = pedido.IdUsuario,
        //            Fecha = DateTime.Now,
        //            Accion = "Nuevo pedido",
        //            Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
        //        };
        //        _context.AddEntity(historialPedido);
        //        foreach (var item in itemsDelCarrito)
        //        {
        //            var detalleHistorialPedido = new DetalleHistorialPedido()
        //            {
        //                HistorialPedidoId = historialPedido.Id,
        //                ProductoId = item.ProductoId,
        //                Cantidad = item.Cantidad ?? 0,
        //            };
        //            _context.Add(detalleHistorialPedido);
        //        }
        //        _context.DeleteRangeEntity(itemsDelCarrito);

        //        // Crear la lista de items para PayPal
        //        if (string.IsNullOrEmpty(moneda))
        //        {
        //            // Puedes establecer un valor predeterminado o devolver un error
        //            moneda = "EUR"; 
        //        }
        //        var items = new List<Item>();
        //        decimal totalAmount = 0;
        //        foreach (var item in itemsDelCarrito)
        //        {
        //            var producto = await _context.Productos.FindAsync(item.ProductoId);
        //            var paypalItem = new Item()
        //            {
        //                name = producto.NombreProducto,
        //                currency = moneda,
        //                price = producto.Precio.ToString("0.00"),
        //                quantity = item.Cantidad.ToString(),
        //                sku = "producto"
        //            };
        //            items.Add(paypalItem);
        //            totalAmount += Convert.ToDecimal(producto.Precio) * Convert.ToDecimal(item.Cantidad ?? 0);

        //        }
        //        var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
        //        string returnUrl;
        //        if (isDocker) 
        //        {
        //            returnUrl = _configuration["Paypal:returnUrlConDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlConDocker");
        //        }
        //        else
        //        {
        //            returnUrl = _configuration["Paypal:returnUrlSinDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlSinDocker");
        //        }

        //        string cancelUrl = "https://localhost:7056/Payment/Cancel";

        //        var createdPayment = await _unitOfWork.PaypalService.CreateOrderAsync(items, totalAmount, returnUrl, cancelUrl, moneda);

        //        //Cuando el pago es exitoso capturamos el valor que tenga approvalUrl y lo mandamos al controlador
        //        string approvalUrl = createdPayment.links.FirstOrDefault(x => x.rel.ToLower() == "approval_url")?.href;
        //        if (!string.IsNullOrEmpty(approvalUrl))
        //        {
        //            return (true, "Redirigiendo a PayPal para completar el pago", approvalUrl);
        //        }

        //        else
        //        {
        //            return (false, "Fallo al iniciar el pago con paypal", null);
        //        }
        //    }
        //    return (true, null, null);
        //}
        public async Task<(bool, string, string)> Pagar(string moneda, int userId)
        {
            var carrito = await ObtenerCarrito(userId);
            if (carrito == null)
            {
                return (false, "El carrito está vacío o no se pudo obtener", null);
            }

            var itemsDelCarrito = await ConvertirItemsAPedido(carrito.Id);
            if (itemsDelCarrito == null || !itemsDelCarrito.Any())
            {
                return (false, "No se pudieron convertir los items a pedido", null);
            }

            var pedido = new Pedido
            {
                NumeroPedido = GenerarNumeroPedido(),
                FechaPedido = DateTime.Now,
                EstadoPedido = "En Proceso",
                IdUsuario = userId
            };
            _context.AddEntity(pedido);

            foreach (var item in itemsDelCarrito)
            {
                var detallePedido = new DetallePedido
                {
                    PedidoId = pedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad ?? 0
                };
                _context.AddEntity(detallePedido);
            }

            var historialPedido = new HistorialPedido
            {
                IdUsuario = pedido.IdUsuario,
                Fecha = DateTime.Now,
                Accion = "Nuevo pedido",
                Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AddEntity(historialPedido);

            foreach (var item in itemsDelCarrito)
            {
                var detalleHistorialPedido = new DetalleHistorialPedido
                {
                    HistorialPedidoId = historialPedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad ?? 0
                };
                _context.Add(detalleHistorialPedido);
            }

            _context.DeleteRangeEntity(itemsDelCarrito);

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
                return (true, "Redirigiendo a PayPal para completar el pago", approvalUrl);
            }

            return (false, "Fallo al iniciar el pago con PayPal", null);
        }

       

        private string GenerarNumeroPedido()
        {
            var length = 10;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public IQueryable<ItemsDelCarrito> ObtenerItems(int id)
        {

            var items = _context.ItemsDelCarritos
                            .Include(i => i.Producto)
                             .Include(i => i.Producto.IdProveedorNavigation)
                            .Where(i => i.CarritoId == id);
            return items;

        }
        public async Task<List<Monedum>> ObtenerMoneda()
        {
            return await _context.Moneda.ToListAsync();
        }
        public async Task<(bool, string)> Incremento(int id)
        {
            var carrito = await ItemsDelCarrito(id);
            if (carrito != null)
            {
                carrito.Cantidad++;
                _context.UpdateEntity(carrito);
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == carrito.ProductoId);
                if (producto != null && producto.Cantidad > 0)
                {
                    // Decrementa la cantidad del producto en la tabla de productos
                    producto.Cantidad--;
                    _context.UpdateEntity(producto);
                }
            }
            return (true, null);
        }
        public async Task<(bool, string)> Decremento(int id)
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
                    _context.UpdateEntity(producto);
                }
                _context.UpdateEntity(carrito);

            }
            return (true, null);
        }
        public async Task<(bool, string)> EliminarProductoCarrito(int id)
        {
            var itemsCarrito = await _context.ItemsDelCarritos.FirstOrDefaultAsync(x => x.Id == id);
            if (itemsCarrito == null)
            {
                return (false, "No se puede eliminar porque no hay productos");
            }
            _context.DeleteEntity(itemsCarrito);
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == itemsCarrito.ProductoId);
            if (producto != null)
            {
                // Incrementa la cantidad del producto en la tabla de productos
                producto.Cantidad++;
                //_context.Productos.Update(producto);
                _context.UpdateEntity(producto);
            }
            return (true, null);
        }
    }
}
