using Aspose.Pdf.Operators;
using GestorInventario.Application.Classes;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PaypalServerSdk.Standard.Models;
using System.Security.Claims;


namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CarritoRepository> _logger;
        private readonly IAdminRepository _admin;
        public CarritoRepository(GestorInventarioContext context, IUnitOfWork unit, IHttpContextAccessor contextAccessor, IConfiguration configuration, ILogger<CarritoRepository> logger, IAdminRepository admin)
        {
            _context = context;
            _unitOfWork = unit;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _logger = logger;
            _admin = admin;
        }
        //Los metodos que estan aqui esta en CarritoController
        public async Task<Carrito> ObtenerCarritoUsuario(int userId)=>await _context.Carritos.FirstOrDefaultAsync(x => x.UsuarioId == userId);
        public async Task<List<ItemsDelCarrito>> ObtenerItemsDelCarritoUsuario(int userIdcarrito)=>await _context.ItemsDelCarritos.Where(i => i.CarritoId == userIdcarrito).ToListAsync();
        public async Task<ItemsDelCarrito> ItemsDelCarrito(int Id)=>await _context.ItemsDelCarritos.FirstOrDefaultAsync(x => x.Id == Id);      
        public async Task<(bool, string, string)> Pagar(string moneda, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nombreCompleto = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
              
                var infoUsuario = new InfoUsuario();
                if (int.TryParse(nombreCompleto, out usuarioId))
                {
                    var usuarioActual = await _admin.ObtenerUsuarioId(usuarioId);

                    // Asignar los valores a la instancia de InfoUsuario
                    infoUsuario.nombreCompletoUsuario = usuarioActual.NombreCompleto;
                    infoUsuario.telefono = usuarioActual.Telefono;
                    infoUsuario.codigoPostal = usuarioActual.CodigoPostal;
                    infoUsuario.ciudad = usuarioActual.Ciudad;

                    string[] direccionParts = usuarioActual.Direccion.Split(",");
                    infoUsuario.line1 = direccionParts.Length > 0 ? direccionParts[0].Trim() : "";
                    infoUsuario.line2 = direccionParts.Length > 1 ? direccionParts[1].Trim() : "";
                }
                //Cuando vas a pagar lo primero que obtenemos es los items del carrito
                var itemsDelCarrito = await ObtenerItemsDelCarrito(userId);
                //Una vez obtenidos los items creamos el pedido
                var historialPedido = await CreacionPedido(userId, itemsDelCarrito);
                //Eliminamos del carrito los items
                await _context.DeleteRangeEntityAsync(itemsDelCarrito);
                moneda = string.IsNullOrEmpty(moneda) ? "EUR" : moneda;
               //Preparamos el pago para paypal
                var checkout = await CrearCheckoutParaPago(itemsDelCarrito, moneda, infoUsuario);
                var createdPaymentJson = await _unitOfWork.PaypalService.CreateOrderAsync(checkout);
                // Deserializar la respuesta
                var createdPayment = JsonConvert.DeserializeObject<PayPalOrderResponse>(createdPaymentJson);
                var links = createdPayment?.links;
                string approvalUrl = links?.FirstOrDefault(x => x.rel == "approval_url")?.href;

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
        private async Task<List<ItemsDelCarrito>> ObtenerItemsDelCarrito(int userId)
        {
            var carrito = await ObtenerCarritoUsuario(userId);


            var itemsDelCarrito = await ObtenerItemsDelCarritoUsuario(carrito.Id);

            return itemsDelCarrito;
        }
        private async Task<HistorialPedido> CreacionPedido(int userId, List<ItemsDelCarrito> itemsDelCarrito)
        {
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
           
            var ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress;
            string ipString = (ip != null && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                ? ip.MapToIPv4().ToString()
                : ip?.ToString();


            var historialPedido = new HistorialPedido
            {
                IdUsuario = pedido.IdUsuario,
                Fecha = DateTime.Now,
                Accion = "Nuevo pedido",
                Ip = ipString
            };
            await _context.AddEntityAsync(historialPedido);
            foreach (var item in itemsDelCarrito)
            {
                var detalleHistorialPedido = new DetalleHistorialPedido
                {
                    HistorialPedidoId = historialPedido.Id,
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad ?? 0,
                    NumeroPedido= pedido.NumeroPedido,
                    FechaPedido=DateTime.Now,
                    EstadoPedido=pedido.EstadoPedido
                   
                };
                await _context.AddEntityAsync(detalleHistorialPedido);
            }
            return historialPedido;
        }
       private async Task<Checkout> CrearCheckoutParaPago(List<ItemsDelCarrito> itemsDelCarrito, string moneda, InfoUsuario infoUsuario)
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

            var checkout = new Checkout
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
            return checkout;
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
