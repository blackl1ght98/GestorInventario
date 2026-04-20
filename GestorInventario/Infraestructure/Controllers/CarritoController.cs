using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.Infraestructure.Controllers
{
    public class CarritoController : Controller
    {       
        private readonly ICarritoRepository _carritoRepository;              
        private readonly ILogger<CarritoController> _logger;        
        private readonly IPolicyExecutor _policyExecutor;       
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IPaymentService _paymentService;
        public CarritoController( ICarritoRepository carritorepository,   ICurrentUserAccessor current,
        ILogger<CarritoController> logger,  IPolicyExecutor executor,  IPaginationHelper pagination, IPaymentService pay)
        {          
            _carritoRepository = carritorepository;       
            _logger = logger;              
            _policyExecutor=executor;           
            _paginationHelper = pagination;
            _currentUserAccessor = current;
            _paymentService = pay;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            try
            {
                int usuarioId = _currentUserAccessor.GetCurrentUserId();

                var resultado = await _policyExecutor.ExecutePolicyAsync(
                    () => _carritoRepository.ObtenerCarritoUsuario(usuarioId)
                );

                var carrito = resultado.Data;

                // Crear carrito automáticamente si no existe
                if (carrito == null)
                {
                    var carritoUsuario = await _policyExecutor.ExecutePolicyAsync(
                        () => _carritoRepository.CrearCarritoUsuario(usuarioId)
                    );
                    carrito = carritoUsuario.Data;
                }

                var itemsDelCarrito = _policyExecutor.ExecutePolicy(
                    () => _carritoRepository.ObtenerItemsConDetalles(carrito.Id)
                );

                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(itemsDelCarrito.Data, paginacion)
                );

               
                var subtotal = paginationResult.Items.Sum(item => item.Producto.Precio * (item.Cantidad ?? 0m));
                var impuestos = subtotal * 0.21m;        // IVA sobre el subtotal
                var total = subtotal + impuestos;        // Total = subtotal + IVA

                var resultadoMonedas = await _policyExecutor.ExecutePolicyAsync(
                    () => _carritoRepository.ObtenerMoneda()
                );

                var viewModel = new CarritoViewModel
                {
                    Productos = paginationResult.Items.ToList(),
                    Subtotal = subtotal,
                    Impuestos = impuestos,          // ← IVA 
                    Shipping = 0m,
                    Total = total,                  // ← Total con IVA
                    Monedas = new SelectList(resultadoMonedas.Data, "Codigo", "Codigo"),
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual
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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string monedaSeleccionada)
        {
            CultureHelper.SetInvariantCulture();
                try
                {
               
               
                    int usuarioId= _currentUserAccessor.GetCurrentUserId();   
                        
                    var resultado = await _policyExecutor.ExecutePolicyAsync(()=> _paymentService.Pagar(monedaSeleccionada, usuarioId))  ;
                    if (resultado.Success)
                    {
                        return Redirect(resultado.Data);
                    }
                    else
                    {
                    _logger.LogError("Ocurrio un error al realizar el pago");
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
        [Authorize]
       
        public async Task<ActionResult> Incrementar(int id)
        {
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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Decrementar(int id)
        {
           

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
        [Authorize]
        
        public async Task<IActionResult> EliminarProductoCarrito(int id)
        {
            try
            {
               
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
