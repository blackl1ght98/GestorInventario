
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.Infraestructure.Controllers
{
    public class CarritoController : Controller
    {       
        private readonly ICarritoRepository _carritoRepository;       
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<CarritoController> _logger;        
        private readonly PolicyExecutor _policyExecutor;
        private readonly UtilityClass _utilityClass;
        private readonly PaginationHelper _paginationHelper;
        public CarritoController( ICarritoRepository carritorepository,  GenerarPaginas generarPaginas, 
        ILogger<CarritoController> logger,  PolicyExecutor executor, UtilityClass utility, PaginationHelper pagination)
        {          
            _carritoRepository = carritorepository;       
            _generarPaginas = generarPaginas;
            _logger = logger;              
            _policyExecutor=executor;
            _utilityClass = utility;
            _paginationHelper = pagination;
        }
       
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            try
            {
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                    return RedirectToAction("Login", "Auth");
                }

                int usuarioId = _utilityClass.ObtenerUsuarioIdActual();
                var resultado = await _policyExecutor.ExecutePolicyAsync(
                    () => _carritoRepository.ObtenerCarritoUsuario(usuarioId)
                );
                var carrito = resultado.Data;
                // 🔹 Crear carrito automáticamente si no existe
                if (carrito == null)
                {
                    carrito = await _policyExecutor.ExecutePolicyAsync(
                        () => _carritoRepository.CrearCarritoUsuario(usuarioId)
                    );
                }

                var itemsDelCarrito = _policyExecutor.ExecutePolicy(
                    () => _carritoRepository.ObtenerItemsConDetalles(carrito.Id)
                );

                // ✅ Aquí usamos el mismo helper que en el controlador de usuarios
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(itemsDelCarrito, paginacion)
                );

                var subtotal = paginationResult.Items.Sum(item => item.Producto.Precio * (item.Cantidad ?? 0));
                var shipping = 2.99m;
                var total = subtotal + shipping;

                var viewModel = new CarritoViewModel
                {
                    Productos = paginationResult.Items.ToList(),
                    Monedas = new SelectList(
                        await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda()),
                        "Codigo",
                        "Codigo"
                    ),
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    Subtotal = subtotal,
                    Shipping = shipping,
                    Total = total
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
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
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                        return RedirectToAction("Login", "Auth");
                    }
               
                    int usuarioId= _utilityClass.ObtenerUsuarioIdActual();   
                        
                    var resultado = await _policyExecutor.ExecutePolicyAsync(()=> _carritoRepository.PagarV2(monedaSeleccionada, usuarioId))  ;
                    if (resultado.Success)
                    {
                        return Redirect(resultado.Data);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = resultado.Message;
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
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Login", "Auth");
            }
            var resultado= await _carritoRepository.Incremento(id);
            if (resultado.Success)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"]=resultado.Message;
            }
           
            return RedirectToAction(nameof(Index));
        }
        //Metodo para decrementar producto
        [HttpPost]
        public async Task<ActionResult> Decrementar(int id)
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Login", "Auth");
            }

            var resultado= await _carritoRepository.Decremento(id);
            if (resultado.Success)
            {
                return RedirectToAction("Index");

            }
            else
            {
                TempData["ErrorMessage"] = resultado.Message;
            }          
            return RedirectToAction("Index");
        }
       //Metodo para eliminar un producto
        [HttpPost]
        public async Task<IActionResult> EliminarProductoCarrito(int id)
        {
            try
            {
                if (!(User.Identity?.IsAuthenticated ?? false))
                {
                    return RedirectToAction("Login", "Auth");
                }
                var resultado = await _carritoRepository.EliminarProductoCarrito(id);
                if (resultado.Success)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = resultado.Message;
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
