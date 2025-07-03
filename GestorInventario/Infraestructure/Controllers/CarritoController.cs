
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;

using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class CarritoController : Controller
    {       
        private readonly ICarritoRepository _carritoRepository;       
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<CarritoController> _logger;
        private readonly IUnitOfWork _unitOfWork;
      
        private readonly PolicyExecutor _policyExecutor;
        public CarritoController( ICarritoRepository carritorepository,  GenerarPaginas generarPaginas, 
        ILogger<CarritoController> logger, IUnitOfWork unitOfWork,  PolicyExecutor executor)
        {          
            _carritoRepository = carritorepository;
          
            _generarPaginas = generarPaginas;
            _logger = logger;
            _unitOfWork = unitOfWork;      
            _policyExecutor=executor;
        }
        //Metodo que muestra los items del carrito
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

                    var carrito = await _policyExecutor.ExecutePolicyAsync(()=> _carritoRepository.ObtenerCarritoUsuario(usuarioId)) ;
                    if (carrito != null)
                    {
                      
                        var itemsDelCarrito = _policyExecutor.ExecutePolicy(()=> _carritoRepository.ObtenerItems(carrito.Id)) ;
                        await HttpContext.TotalPaginas(itemsDelCarrito, paginacion.CantidadAMostrar);
                        var productoPaginado = itemsDelCarrito.Paginar(paginacion).ToList();
                        var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
                        ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);
                        ViewData["Moneda"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(()=> _carritoRepository.ObtenerMoneda()) , "Codigo", "Codigo");

                      
                        return View(productoPaginado);
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los productos del carrito");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo que realiza el pago
        [HttpPost]
        public async Task<IActionResult> Checkout(string monedaSeleccionada)
        {
               //Necesario para que paypal entienda el precio
               System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;     
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
                   
                    var (success, message, approvalUrl) = await _policyExecutor.ExecutePolicyAsync(()=> _carritoRepository.PagarV2(monedaSeleccionada, usuarioId))  ;
                    if (success)
                    {
                        return Redirect(approvalUrl);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = message;
                    }
                    ViewData["Moneda"] = new SelectList(await _carritoRepository.ObtenerMoneda(), "Codigo", "Codigo");

                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el checkout");
                return RedirectToAction("Error", "Home");
            }

        }
        //Metodo para incrementar  productos
        [HttpPost]
        public async Task<ActionResult> Incrementar(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }
            var (success, errorMessage)= await _carritoRepository.Incremento(id);
            if (success)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"]=errorMessage;
            }
           
            return RedirectToAction(nameof(Index));
        }
        //Metodo para decrementar producto
        [HttpPost]
        public async Task<ActionResult> Decrementar(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Auth");
            }

            var (success,errorMessage)= await _carritoRepository.Decremento(id);
            if (success)
            {
                return RedirectToAction("Index");

            }
            else
            {
                TempData["ErrorMessage"] = errorMessage;
            }          
            return RedirectToAction("Index");
        }
       //Metodo para eliminar un producto
        [HttpPost]
        public async Task<IActionResult> EliminiarProductoCarrito(int id)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }
                var (success, errorMessage) = await _carritoRepository.EliminarProductoCarrito(id);
                if (success)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = errorMessage;
                }
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el checkout");
                return RedirectToAction("Error", "Home");
            }

        }
        
    }
}
