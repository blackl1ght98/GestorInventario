using GestorInventario.Application.DTOs.Rembolso;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers
{
    public class RembolsoController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IRembolsoRepository _rembolsoRepository;       
        private readonly ILogger<RembolsoController> _logger;
        private readonly IPaginationHelper _paginationHelper;

        public RembolsoController(IPolicyExecutor policyExecutor, IRembolsoRepository rembolsoRepository, 
             ILogger<RembolsoController> logger, IPaginationHelper paginationHelper)
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;
           
            _logger = logger;
            _paginationHelper = paginationHelper;

        }
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {


                var queryable = await _policyExecutor.ExecutePolicyAsync(() => _rembolsoRepository.ObtenerRembolsos());
                if (!string.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NumeroPedido.Contains(buscar));
                }



                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(queryable, paginacion)
                );

                var viewModel = new RembolsosViewModel
                {
                    Rembolsos = paginationResult.Items, 
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginacion.Pagina,
                    Buscar = buscar
                };

            

                return View(viewModel);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpDelete]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarRembolso([FromBody] RembolsoRequestDto request)
        {
            var success = await _policyExecutor.ExecutePolicyAsync(() => _rembolsoRepository.EliminarRembolso(request.Id));
            if (success.Success)
            {
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = success.Message;
                return Json(new { success = false, errorMessage = success.Message });
            }
        }

    }
}
