using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.MetodosExtension.Tabla_Items_Carrito;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal.Api;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class CarritoController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly IAdminRepository _adminRepository;
        private readonly IAdminCrudOperation _admincrudOperation;
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<CarritoController> _logger;
        private readonly IUnitOfWork _unitOfWork;
     
        public CarritoController(GestorInventarioContext context, IAdminRepository adminrepository, IAdminCrudOperation admincrudOperation, GenerarPaginas generarPaginas, ILogger<CarritoController> logger, IUnitOfWork unitOfWork)
        {
            _context = context;
            _adminRepository = adminrepository;
            _admincrudOperation = admincrudOperation;
            _generarPaginas = generarPaginas;
            _logger = logger;
            _unitOfWork = unitOfWork;
          
        }

        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            try
            {
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var carrito = await _adminRepository.ObtenerCarrito(usuarioId);
                    //var carrito = await _context.Carritos.FindByUserId(usuarioId);
                    // var carrito = _context.Carritos.FindByUserId(usuarioId);
                    //var carrito = await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                    if (carrito != null)
                    {
                        var itemsDelCarrito = _context.ItemsDelCarritos
                            .Include(i => i.Producto)

                             .Include(i => i.Producto.IdProveedorNavigation)

                            .Where(i => i.CarritoId == carrito.Id);

                        //var itemsDelCarrito = await _adminRepository.ObtenerItemsCarrito(carrito.Id);
                        await HttpContext.InsertarParametrosPaginacionRespuesta(itemsDelCarrito, paginacion.CantidadAMostrar);
                        var productoPaginado = itemsDelCarrito.Paginar(paginacion).ToList();
                        var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                        ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);


                        // Pasa los productos a la vista
                        return View(productoPaginado);
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los productos del carrito");
                return BadRequest("Error al mostrar los productos del carrito intentelo de nuevo mas tarde o si el problema persiste contacte con el administrador ");
            }
          
        }

        public async Task<IActionResult> Checkout()
        {
            // Cambia la cultura actual del hilo a InvariantCulture
                    System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //        // Cambia la cultura de la interfaz de usuario actual del hilo a InvariantCulture
                  System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var carrito = await _adminRepository.ObtenerCarrito(usuarioId);

                    if (carrito != null)
                    {
                        var itemsDelCarrito = await _adminRepository.ConvertirItemsAPedido(carrito.Id);

                        var pedido = new Pedido
                        {
                            NumeroPedido = GenerarNumeroPedido(),
                            FechaPedido = DateTime.Now,
                            EstadoPedido = "Pendiente",
                            IdUsuario = usuarioId
                        };
                        _context.AddEntity(pedido);

                       
                        // Este bucle se va a recorrer como items existan en el carrito
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

                        // Aquí agregamos el pedido al historial de pedidos
                        var historialPedido = new HistorialPedido()
                        {
                            NumeroPedido = pedido.NumeroPedido,
                            FechaPedido = pedido.FechaPedido,
                            EstadoPedido = pedido.EstadoPedido,
                            IdUsuario = pedido.IdUsuario,
                        };
                        _context.Add(historialPedido);
                        await _context.SaveChangesAsync();

                        // Lógica para los detalles de productos (si es necesario)
                        foreach (var item in itemsDelCarrito)
                        {
                            var detalleHistorialPedido = new DetalleHistorialPedido()
                            {
                                HistorialPedidoId = historialPedido.Id,
                                ProductoId = item.ProductoId,
                                Cantidad = item.Cantidad ?? 0,
                            };
                            _context.Add(detalleHistorialPedido);
                        }

                        _context.DeleteRangeEntity(itemsDelCarrito);
                        // Crear la lista de items para PayPal
                        var items = new List<Item>();
                        decimal totalAmount = 0;
                        foreach (var item in itemsDelCarrito)
                        {
                            var producto = await _context.Productos.FindAsync(item.ProductoId);
                            var paypalItem = new Item()
                            {
                                name = producto.NombreProducto,
                                currency = "EUR",
                                price = producto.Precio.ToString("0.00"),
                                quantity = item.Cantidad.ToString(),
                                sku = "producto"
                            };
                            items.Add(paypalItem);


                            // Calcular el precio total
                            // Calcular el precio total
                            // await _divisaConverter.UpdateExchangeRates();
                            totalAmount += Convert.ToDecimal(producto.Precio) * Convert.ToDecimal(item.Cantidad ?? 0);



                        }

                        string returnUrl = "https://localhost:7056/Payment/Success";
                        string cancelUrl = "https://localhost:7056/Payment/Cancel";

                        // Crear el pago con PayPal
                        var createdPayment = await _unitOfWork.PaypalService.CreateOrderAsync(items, totalAmount, returnUrl, cancelUrl, "EUR"); // Asegúrate de usar la moneda correcta aquí
                                                                                                                                                // Obtener el enlace de aprobación de PayPal
                        string approvalUrl = createdPayment.links.FirstOrDefault(x => x.rel.ToLower() == "approval_url")?.href;
                        if (!string.IsNullOrEmpty(approvalUrl))
                        {
                            // Redirigir al usuario a PayPal para completar el pago
                            return Redirect(approvalUrl);
                        }
                        else
                        {
                            TempData["error"] = "Fallo al iniciar el pago con PayPal";
                        }
                        TempData["SuccessMessage"] = "El pedido se ha creado con éxito.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el checkout");
                return BadRequest("Error al realizar el checkout intentelo de nuevo mas tarde o si el problema persiste contacte con el administrador");
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
        [HttpPost]
        public async Task<ActionResult> Incrementar(int id)
        {
            // Busca el producto en la base de datos
            var carrito= await _adminRepository.ItemsDelCarrito(id);
            //var carrito = await _context.ItemsDelCarritos.ItemsCarritoIds(id);
            //var carrito = await _context.ItemsDelCarritos.FirstOrDefaultAsync(p => p.Id == id);

            if (carrito != null)
            {
              
                carrito.Cantidad++;
                _context.UpdateEntity(carrito);
                //_context.ItemsDelCarritos.Update(carrito);
                //await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Decrementar(int id)
        {
            var carrito = await _adminRepository.ItemsDelCarrito(id);

            //var carrito = await _context.ItemsDelCarritos.ItemsCarritoIds(id);

            // Busca el producto en la base de datos
            //var carrito = await _context.ItemsDelCarritos.FirstOrDefaultAsync(p => p.Id == id);

            if (carrito != null)
            {
                // Decrementa la cantidad del producto en el carrito
                carrito.Cantidad--;

                // Busca el producto correspondiente en la tabla de productos
                var producto = await _adminRepository.Decrementar(carrito.ProductoId);
                //var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == carrito.ProductoId);

                if (producto != null)
                {
                    // Incrementa la cantidad del producto en la tabla de productos
                    producto.Cantidad++;
                    //_context.Productos.Update(producto);
                    _context.UpdateEntity(producto);
                }
                _context.UpdateEntity(carrito);

                //_context.ItemsDelCarritos.Update(carrito);
                //await _context.SaveChangesAsync();
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }

    }
}
