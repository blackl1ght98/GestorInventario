using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.MetodosExtension.Tabla_Items_Carrito;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public CarritoController(GestorInventarioContext context, IAdminRepository adminrepository, IAdminCrudOperation admincrudOperation, GenerarPaginas generarPaginas, ILogger<CarritoController> logger)
        {
            _context = context;
            _adminRepository = adminrepository;
            _admincrudOperation = admincrudOperation;
            _generarPaginas = generarPaginas;
            _logger = logger;
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
            try
            {
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var carrito = await _adminRepository.ObtenerCarrito(usuarioId);

                    //var carrito = await _context.Carritos.FindByUserId(usuarioId);

                    // var carrito = await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
                    if (carrito != null)
                    {
                        var itemsDelCarrito = await _adminRepository.ConvertirItemsAPedido(carrito.Id);
                        //var itemsDelCarrito = await _context.ItemsDelCarritos
                        //    .Where(i => i.CarritoId == carrito.Id)
                        //    .ToListAsync();

                        var pedido = new Pedido
                        {
                            NumeroPedido = GenerarNumeroPedido(),
                            FechaPedido = DateTime.Now,
                            EstadoPedido = "Pendiente",
                            IdUsuario = usuarioId
                        };
                        _context.AddEntity(pedido);
                        //_context.Pedidos.Add(pedido);
                        //await _context.SaveChangesAsync();
                        //Este bucle se va a recorrer como items existan en el carrito
                        foreach (var item in itemsDelCarrito)
                        {
                            var detallePedido = new DetallePedido
                            {
                                PedidoId = pedido.Id,
                                ProductoId = item.ProductoId,
                                Cantidad = item.Cantidad ?? 0
                            };
                            _context.AddEntity(detallePedido);
                            //_context.DetallePedidos.Add(detallePedido);
                        }
                        _context.DeleteRangeEntity(itemsDelCarrito);
                        // _context.ItemsDelCarritos.RemoveRange(itemsDelCarrito);
                        //await _admincrudOperation.SaveChangesAsync();
                        // await _context.SaveChangesAsync();

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
