using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Application.Interfaces.Application.Services.BackgroundServices;
using GestorInventario.Application.Interfaces.Application.Services.Refunds;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.MetodosPaginacion;
using GestorInventario.Application.Interfaces.Notifications.SendNotification.Email;
using GestorInventario.Domain.Common;
using GestorInventario.Extensions;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Refunds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GestorInventario.Controllers.RembolsoController
{
    public class RembolsoController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IRefundRepository _refundRepository;
        private readonly ILogger<RembolsoController> _logger;
        private readonly IPaginationService _paginationService;
        private readonly IRefundService _refundService;
        private readonly IBackgroundTaskQueue _background;

        

        public RembolsoController(
            IPolicyExecutor policyExecutor,
            IRefundRepository refundRepository,
            ILogger<RembolsoController> logger,
            IPaginationService paginationService,
            IRefundService refundService,
            IBackgroundTaskQueue provider
           )
        {
            _policyExecutor = policyExecutor;
            _refundRepository = refundRepository;
            _logger = logger;
            _paginationService = paginationService;
            _refundService = refundService;
            _background = provider;
          
        }

        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var queryable = await _policyExecutor.ExecutePolicyAsync(() => _refundRepository.ObtenerRembolsos());
                if (!string.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NumeroPedido.Contains(buscar));
                }

                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationService.PaginarAsync(queryable, paginacion)
                );

                var viewModel = new RefundsViewModel
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

        [HttpDelete("{id}")]
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> EliminarRembolso(int id)
        {
            var success = await _policyExecutor.ExecutePolicyAsync(() => _refundRepository.EliminarRembolso(id));

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

        [HttpPost]
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> RefundSale([FromBody] RefundFullDto request)
        {
            if (request == null || request.PedidoId <= 0)
                return BadRequest("Datos inválidos");

            try
            {
                var result = await _refundService.EjecutarReembolsoTotalAsync(request);
                var finalResult = result.Unwrap();
                if (result.Success)
                {
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IRefundNotification>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            finalResult.PedidoId,
                            finalResult.DetallesIds,
                            finalResult.PedidoId,
                            "Reembolso Aprobado");
                    });
                    return Ok(new { success = true });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en refund order {PedidoId}", request.PedidoId);
                return StatusCode(500, new { success = false, message = "Error procesando reembolso" });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> RefundPartial([FromBody] RefundPartialDto request)
        {
            if (request?.DetalleId <= 0)
            {
                return Json(new { success = false, message = "Solicitud inválida." });
            }

            try
            {
                var resultado = await _refundService.RealizarRembolsoParcial(request);

                var total = CalculadoraFiscal.CalcularPrecioConIva(resultado.Data.precioProducto);


                if (resultado.Success)
                {
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IRefundNotification>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            resultado.Data.pedidoId,
                            new[] { resultado.Data.detalleId },
                            total,
                            resultado.Data.motivo);
                    });
                    return Json(new { success = true, message = "rembolso realizado con exito" });
                }
                else
                {
                    return Json(new { success = false, message = "Ocurrio un error al realizar el rembolso" });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación fallida en reembolso parcial order {PedidoId}", request.DetalleId);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en reembolso parcial order {PedidoId}", request.DetalleId);
                return Json(new
                {
                    success = false, message = "No se pudo realizar el reembolso. Intenta de nuevo o contacta soporte."
                });
            }
        }

        [Authorize]
        public IActionResult FormularioRembolso()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormularioRembolso(RefundFormViewModel form)
        {
            try
            {
                var dto = new RefundDto
                {
                    NumeroPedido = form.NumeroPedido,
                    NombreCliente = form.NombreCliente,
                    EmailCliente = form.EmailCliente,
                    FechaRembolso = form.FechaRembolso,
                    MotivoRembolso = form.MotivoRembolso,
                };

                var refund = await _refundService.RefundFormAsync(dto);
                if (refund.Success && User.IsAdministrador() )
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Pedido");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el reembolso");
                return StatusCode(500, "Ocurrió un error al procesar tu solicitud");
            }
        }
    }
}