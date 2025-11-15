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

        public RembolsoController(PolicyExecutor policyExecutor, IRembolsoRepository rembolsoRepository, GenerarPaginas generarPaginas, ILogger<RembolsoController> logger)
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;
            _generarPaginas = generarPaginas;
            _logger = logger;
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

           
              

                var (rembolsos, totalItems) = await _policyExecutor.ExecutePolicyAsync(() => queryable.PaginarAsync(paginacion));
                var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);
                var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

                var viewModel = new RembolsosViewModel
                {
                    Rembolsos = rembolsos, 
                    Paginas = paginas,
                    TotalPaginas = totalPaginas,
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
