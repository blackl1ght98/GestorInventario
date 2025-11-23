using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers
{
    public class RembolsoController : Controller
    {
        private readonly PolicyExecutor _policyExecutor;
        private readonly IRembolsoRepository _rembolsoRepository;
        private readonly GenerarPaginas _generarPaginas;
        private readonly ILogger<RembolsoController> _logger;
        private readonly PaginationHelper _paginationHelper;

        public RembolsoController(PolicyExecutor policyExecutor, IRembolsoRepository rembolsoRepository, 
            GenerarPaginas generarPaginas, ILogger<RembolsoController> logger, PaginationHelper paginationHelper)
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;
            _generarPaginas = generarPaginas;
            _logger = logger;
            _paginationHelper = paginationHelper;

        }

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
        public async Task<IActionResult> EliminarRembolso([FromBody] RembolsoRequest request)
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
