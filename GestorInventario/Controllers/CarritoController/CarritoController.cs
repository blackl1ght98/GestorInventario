using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.Payment;
using GestorInventario.Interfaces.Application.Services.ShopCart;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;

using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.ShoppingCart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.Controllers.CarritoController
{
    public class CarritoController : Controller
    {       
        private readonly ICarritoRepository _carritoRepository;              
        private readonly ILogger<CarritoController> _logger;        
        private readonly IPolicyExecutor _policyExecutor;       
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IPaymentService _paymentService;
        private readonly ICarritoService _carritoService;
        public CarritoController(
         ICarritoRepository carritorepository,  
         ICurrentUserAccessor currentUser, 
         ICarritoService carritoService,
         ILogger<CarritoController> logger,  
         IPolicyExecutor policyExecutor,  
         IPaginationHelper pagination, 
         IPaymentService paymentService)
        {          
            _carritoRepository = carritorepository;       
            _logger = logger;              
            _policyExecutor= policyExecutor;           
            _paginationHelper = pagination;
            _currentUserAccessor = currentUser;
            _paymentService = paymentService;
            _carritoService=carritoService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index([FromQuery] Paginacion paginacion)
        {
            try
            {
                int usuarioId = _currentUserAccessor.GetCurrentUserId();

                var carrito = await _policyExecutor.ExecutePolicyAsync(
                    () => _carritoRepository.ObtenerCarritoUsuario(usuarioId)
                );

              

                // Crear carrito automáticamente si no existe
                if (carrito == null)
                {
                    var carritoUsuario = await _policyExecutor.ExecutePolicyAsync(
                        () => _carritoService.CrearCarritoUsuario(usuarioId)
                    );
                    carrito = carritoUsuario.Data;
                }

                var itemsDelCarrito = _policyExecutor.ExecutePolicy(
                    () => _carritoRepository.ObtenerItemsConDetalles(carrito.Id)
                );

                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(itemsDelCarrito, paginacion)
                );

               
                var subtotal = paginationResult.Items.Sum(item => item.Producto.Precio * (item.Cantidad));
                var impuestos = subtotal * 0.21m;        
                var total = subtotal + impuestos;        

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
                    Monedas = new SelectList(resultadoMonedas, "Codigo", "Codigo"),
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
                int usuarioId = _currentUserAccessor.GetCurrentUserId();

                var resultado = await _policyExecutor.ExecutePolicyAsync(() => _paymentService.Pagar(monedaSeleccionada, usuarioId));
                if (resultado.Success)
                {
                    return Redirect(resultado.Data);
                }
                else
                {
                    _logger.LogError("Ocurrio un error al redireccionar a paypal");
                    return RedirectToAction("Index", "Home");
                }

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
            var resultado= await _carritoService.Incremento(id);
            if (resultado.Success)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"]=resultado.Message;
                return RedirectToAction(nameof(Index));
            }                
        }
        //Metodo para decrementar producto
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Decrementar(int id)
        {
           

            var resultado= await _carritoService.Decremento(id);
            if (resultado.Success)
            {
                return RedirectToAction("Index");

            }
            else
            {
                TempData["ErrorMessage"] = resultado.Message;
                return RedirectToAction("Index");
            }                     
        }
       //Metodo para eliminar un producto
        [HttpPost]
        [Authorize]
        
        public async Task<IActionResult> EliminarProductoCarrito(int id)
        {
            try
            {
               
                var resultado = await _carritoService.EliminarProductoCarrito(id);
                if (resultado.Success)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = resultado.Message;
                    return RedirectToAction("Index");
                }             
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el checkout");
                return RedirectToAction("Error", "Home");
            }

        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int idProducto, int cantidad)
        {
            try
            {


                int usuarioId = _currentUserAccessor.GetCurrentUserId();

                var success = await _policyExecutor.ExecutePolicyAsync(() => _carritoService.AgregarProductoAlCarrito(idProducto, cantidad, usuarioId));
                if (success.Success)
                {
                    return RedirectToAction("Index", "Productos");

                }
                else
                {
                    TempData["ErrorMessage"] = success.Message;
                }

                return RedirectToAction("Index","Productos");
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al agregar el producto al carrito");
                return RedirectToAction("Error", "Home");
            }
        }

    }
}
