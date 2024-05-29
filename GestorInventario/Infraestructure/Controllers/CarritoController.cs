using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;

using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PayPal.Api;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class CarritoController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly ICarritoRepository _carritoRepository;
        
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<CarritoController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PolicyHandler _PolicyHandler;
        public CarritoController(GestorInventarioContext context, ICarritoRepository carritorepository,  GenerarPaginas generarPaginas, 
        ILogger<CarritoController> logger, IUnitOfWork unitOfWork, PolicyHandler policy)
        {
            _context = context;
            _carritoRepository = carritorepository;
           _PolicyHandler = policy;
            _generarPaginas = generarPaginas;
            _logger = logger;
            _unitOfWork = unitOfWork;
          
        }

        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {

                    var carrito = await ExecutePolicyAsync(()=> _carritoRepository.ObtenerCarrito(usuarioId)) ;
                    if (carrito != null)
                    {
                      
                        var itemsDelCarrito = ExecutePolicy(()=> _carritoRepository.ObtenerItems(carrito.Id)) ;
                        await HttpContext.InsertarParametrosPaginacionRespuesta(itemsDelCarrito, paginacion.CantidadAMostrar);
                        var productoPaginado = itemsDelCarrito.Paginar(paginacion).ToList();
                        var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                        ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                        ViewData["Moneda"] = new SelectList(await ExecutePolicyAsync(()=> _carritoRepository.ObtenerMoneda()) , "Codigo", "Codigo");

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
        
        [HttpPost]
        public async Task<IActionResult> Checkout(string monedaSeleccionada)
        {
            // Cambia la cultura actual del hilo a InvariantCulture
               System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                    // Cambia la cultura de la interfaz de usuario actual del hilo a InvariantCulture
                  System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                   
                    var (success, message, approvalUrl) = await ExecutePolicyAsync(()=> _carritoRepository.Pagar(monedaSeleccionada, usuarioId))  ;
                    if (success)
                    {
                        return Redirect(approvalUrl);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = message;
                    }
                    ViewData["Moneda"] = new SelectList(_context.Moneda, "Codigo", "Codigo");

                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el checkout");
                return BadRequest("Error al realizar el checkout intentelo de nuevo mas tarde o si el problema persiste contacte con el administrador");
            }

        }



        [HttpPost]
        public async Task<ActionResult> Incrementar(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
            // Busca el producto en la base de datos
            var carrito = await ExecutePolicyAsync(() => _carritoRepository.ItemsDelCarrito(id));

            if (carrito != null)
            {
                carrito.Cantidad++;
                _context.UpdateEntity(carrito);

                // Busca el producto en la tabla de productos
                var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == carrito.ProductoId);
                if (producto != null && producto.Cantidad > 0)
                {
                    // Decrementa la cantidad del producto en la tabla de productos
                    producto.Cantidad--;
                    _context.UpdateEntity(producto);
                }
            }

            // Redirige al usuario a la página de índice
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<ActionResult> Decrementar(int id)
        {
            
            var carrito = await ExecutePolicyAsync(()=> _carritoRepository.ItemsDelCarrito(id)) ;

          

            // Busca el producto en la base de datos
          

            if (carrito != null)
            {
                // Decrementa la cantidad del producto en el carrito
                carrito.Cantidad--;

                // Busca el producto correspondiente en la tabla de productos
                var producto = await  ExecutePolicyAsync(()=> _carritoRepository.Decrementar(carrito.ProductoId)) ;
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
        private async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }
        private T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }
    }
}
