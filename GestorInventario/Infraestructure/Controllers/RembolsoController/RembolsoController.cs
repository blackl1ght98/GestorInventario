using GestorInventario.Application.DTOs.Rembolso;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Rembolsos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GestorInventario.Infraestructure.Controllers.RembolsoController
{
   
    public class RembolsoController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IRembolsoRepository _rembolsoRepository;       
        private readonly ILogger<RembolsoController> _logger;
        private readonly IPaginationHelper _paginationHelper;
      
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaymentService _paymentService;
        private readonly IPayPalOrderMappingService _mappingService;   
        private readonly IPedidoManagementService _pedidoService;
        private readonly IBackgroundTaskQueue _background;
        private readonly IPaypalService _paypalService;
        private readonly IPaypalPartialRefundService _paypalPartialRefundService;
        private readonly IPaypalFullRefundService _paypalFullRefundService;
        public RembolsoController(
            IPolicyExecutor policyExecutor, 
            IRembolsoRepository rembolsoRepository, 
             ILogger<RembolsoController> logger, 
             IPaginationHelper paginationHelper,
            
             IPedidoRepository pedidoRepository,
             ICurrentUserAccessor currentUserAccessor,
             IPaypalOrderService paypalOrderService,
             IPaymentService paymentService,
             IPayPalOrderMappingService mappingService,
             IPedidoManagementService pedido,
             IBackgroundTaskQueue provider,
             IPaypalService paypalService,
             IPaypalPartialRefundService paypalPartialRefundService,
             IPaypalFullRefundService paypalFullRefundService)
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;  
            _logger = logger;
            _paginationHelper = paginationHelper;
            _pedidoRepository = pedidoRepository;
            _currentUserAccessor = currentUserAccessor;
            _paypalOrderService = paypalOrderService;
            _paymentService = paymentService;
            _mappingService = mappingService;      
            _pedidoService = pedido;
            _background = provider;
            _paypalService = paypalService;
            _paypalPartialRefundService = paypalPartialRefundService;
            _paypalFullRefundService= paypalFullRefundService;

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
                var result = await _paypalFullRefundService.RefundSaleAsync(request.PedidoId, request.Currency);

                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Message });

                await _pedidoService.ProcesarRembolsoAsync(
                    result.Data.pedidoId,
                    EstadoPedido.Rembolsado.ToString(),
                    result.Data.refundId);

                _background.Enqueue(async (sp, ct) =>
                {
                    var emailService = sp.GetRequiredService<IReembolsoNotificationService>();
                    await emailService.EnviarEmailNotificacionRembolso(
                        result.Data.pedidoId,
                        result.Data.totalAmount,
                        "Rembolso Aprobado");
                });

                return Ok(new { success = true, refundId = result.Data.refundId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en refund pedido {PedidoId}", request.PedidoId);
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
                var result =  await _paypalPartialRefundService.RefundPartialAsync(request.PedidoId, request.Currency, request.Motivo);


                await _paypalService.RegistrarReembolsoParcialAsync(
                    result.Data.pedidoId,
                    result.Data.detalleId,
                    result.Data.refundId,
                    result.Data.montoRembolsado,
                    result.Data.motivo,
                    result.Data.estadoVenta
                );
                _background.Enqueue(async (sp, ct) =>
                {
                    var emailService = sp.GetRequiredService<IReembolsoNotificationService>();
                    await emailService.EnviarEmailNotificacionRembolso(
                    result.Data.pedidoId,
                     result.Data.precioProducto,
                     result.Data.motivo
                );
                });
               

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

                var paymentDetail = _mappingService.MapearOrdenADetallePago(detallespago);

                // Lista para almacenar los ítems de PayPal
                var paypalItems = await _paymentService.ProcesarRembolso(firstPurchaseUnit, paymentDetail, usuarioActual, form, obtenerNumeroPedido, emailCliente);
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
