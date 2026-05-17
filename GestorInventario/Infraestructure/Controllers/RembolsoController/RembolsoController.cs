using GestorInventario.Application.DTOs.Rembolso;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Rembolsos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.RembolsoController
{
   
    public class RembolsoController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IRembolsoRepository _rembolsoRepository;       
        private readonly ILogger<RembolsoController> _logger;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IPaypalRefundService _paypalRefundService;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaymentService _paymentService;
        public RembolsoController(
            IPolicyExecutor policyExecutor, 
            IRembolsoRepository rembolsoRepository, 
             ILogger<RembolsoController> logger, 
             IPaginationHelper paginationHelper,
             IPaypalRefundService paypalRefundService,
             IPedidoRepository pedidoRepository,
             ICurrentUserAccessor currentUserAccessor,
             IPaypalOrderService paypalOrderService,
             IPaymentService paymentService)
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;  
            _logger = logger;
            _paginationHelper = paginationHelper;
            _paypalRefundService = paypalRefundService;
            _pedidoRepository = pedidoRepository;
            _currentUserAccessor = currentUserAccessor;
            _paypalOrderService = paypalOrderService;
            _paymentService = paymentService;

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
        [HttpDelete("{id}")]   
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarRembolso(int id)
        {
            var success = await _policyExecutor.ExecutePolicyAsync(() => _rembolsoRepository.EliminarRembolso(id));

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundSale([FromBody] RefundRequestModelDto request)
        {
            if (request == null || request.PedidoId <= 0)
                return BadRequest("Datos inválidos");

            try
            {
                await _paypalRefundService.RefundSaleAsync(request.PedidoId, request.Currency);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundPartial([FromBody] RefundRequestModelDto request)
        {
            if (request?.PedidoId <= 0)
            {
                return Json(new { success = false, message = "Solicitud inválida." });
            }

            try
            {
                await _paypalRefundService.RefundPartialAsync(request.PedidoId, request.Currency, request.Motivo);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
                var obtenerNumeroPedido = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerNumeroPedido(form));

                if (obtenerNumeroPedido == null)
                {
                    _logger.LogInformation("El numero de pedido proporcionado no existe " + obtenerNumeroPedido);
                    return RedirectToAction(nameof(FormularioRembolso));
                }

                int usuarioActual = _currentUserAccessor.GetCurrentUserId();

                var emailCliente = _policyExecutor.ExecutePolicy(() => _currentUserAccessor.GetCurrentUserEmail());
                if (emailCliente == null)
                {
                    _logger.LogInformation("El email proporcionado no se encuentra registrado " + emailCliente);
                }


                var pedido = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerNumeroPedido(form));

                if (pedido == null)
                {

                    _logger.LogInformation("El pedido con el numero de pedido proporcionado no existe ");
                    return View(nameof(FormularioRembolso));
                }

                var detallespago = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(pedido.OrderId));

                if (detallespago == null)
                {
                    _logger.LogInformation("No se ha podido obtener los detalles del pago");
                    return View(nameof(FormularioRembolso));
                }

                // Verificar que hay purchase units
                if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
                {
                    _logger.LogInformation("No se encuntran las unidades de pago en la peticion");
                }

                var firstPurchaseUnit = detallespago.PurchaseUnits.First();

                var detallesSuscripcion = _paymentService.ProcesarDetallesRembolsoAsync(detallespago);

                // Lista para almacenar los ítems de PayPal
                var paypalItems = await _paymentService.ProcesarRembolso(firstPurchaseUnit, detallesSuscripcion.Data, usuarioActual, form, obtenerNumeroPedido, emailCliente);
                if (User.IsAdministrador())
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Pedido");
                }


            }
            catch (Exception ex)
            {
                // Loggear el error
                _logger.LogError(ex, "Error al procesar el reembolso");
                return StatusCode(500, "Ocurrió un error al procesar tu solicitud");
            }
        }

    }
}
