using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Paypal;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.PaypalControllers
{
    public class PaypalProductController : Controller
    {
       
       
        
        private readonly ILogger<PaypalProductController> _logger;
      
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaypalSubscriptionService _paypalSubscriptionService;         
        private readonly IPaginationHelper _paginationHelper;
     
        
        private readonly IPayPalMappingUtils _map;
       
        public PaypalProductController(
         ILogger<PaypalProductController> logger,
         IPolicyExecutor policyExecutor,
         IPaginationHelper paginationHelper,
         IPaypalSubscriptionService paypalSubscriptionService,
         IPayPalMappingUtils map)
        {
            _paypalSubscriptionService = paypalSubscriptionService;        
           _policyExecutor= policyExecutor;
            _logger = logger;               
            _paginationHelper = paginationHelper;
            _map = map;
            
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MostrarProductos([FromQuery] Paginacion paginacion)
        {
            try
            {
                

             

                // Obtener productos de PayPal
                var (respuestaProductos, tienePaginaSiguiente) = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalSubscriptionService.GetProductsAsync(paginacion.Pagina,paginacion.CantidadAMostrar));

                // Mapear productos a DTO
                var productos = respuestaProductos?.Products?.Select(p => new ProductoPaypalDto
                {
                    Id = p.Id,
                    Nombre = p.Name,
                    Descripcion = p.Description
                }).ToList() ?? new List<ProductoPaypalDto>();

                // Usar el helper para generar la paginación (modo "sin total real")
                var paginationResult = _paginationHelper.PaginarSinTotal(
                    items: productos,
                    paginaActual: paginacion.Pagina,
                    hasNextPage: tienePaginaSiguiente,
                    cantidadAMostrar: paginacion.CantidadAMostrar,
                    radio: paginacion.Radio
                );

                // Crear el ViewModel usando el resultado del helper
                var model = new ProductosPaypalViewModel
                {
                    Productos = paginationResult.Items,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginationResult.PaginaActual,
                    TienePaginaSiguiente = paginationResult.Paginas.LastOrDefault()?.Habilitada ?? false,
                    TienePaginaAnterior = paginacion.Pagina > 1
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los productos de PayPal");
                return RedirectToAction("Error", "Home");
            }
        }
       
        public IActionResult EditarProductoPaypal()
        {
            return View();
        }
       
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditarProductoPaypal(string id, EditProductPaypal model)
        {

            if (ModelState.IsValid)
            {

                try
                {
                    
                    var productResponse = await _paypalSubscriptionService.EditarProducto(id, model.Name, model.Description);
                    return RedirectToAction(nameof(MostrarProductos));
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al crear el plan de suscripción: {ex.Message}");
                }
            }

            return View(model);
        }

       

    }
}