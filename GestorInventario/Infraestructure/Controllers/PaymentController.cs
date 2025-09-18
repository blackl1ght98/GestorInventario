
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaypalService _paypalService;        
       
        private readonly IMemoryCache _memory;
         
        private readonly PolicyExecutor _policyExecutor;
        private readonly IPaymentRepository _paymentRepository;
        private readonly UtilityClass _utilityClass;
        public PaymentController(ILogger<PaymentController> logger,  IMemoryCache memory, 
            PolicyExecutor executor, IPaypalService service, IPaymentRepository payment, UtilityClass utility)
        {
            _logger = logger;                               
            _memory = memory;          
            _policyExecutor = executor;
            _paypalService = service;
            _paymentRepository = payment;
            _utilityClass = utility;
        }
        public async Task<IActionResult> Success()
        {
            try
            {
                if (!_memory.TryGetValue("PayPalOrderId", out string orderId) || string.IsNullOrEmpty(orderId))
                {
                    throw new Exception("No se encontró el ID del pedido en el caché.");
                }

                var (captureId, total, currency) = await _paypalService.CapturarPagoAsync(orderId);

                var usuarioActual = _utilityClass.ObtenerUsuarioIdActual();

                var pedido =  await _paymentRepository.AgregarInfoPedido(usuarioActual,captureId,total,currency,orderId);
         
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }

        //Si el pago es reembolsado
        [HttpPost]
        public async Task<IActionResult> RefundSale(RefundRequestModel request)
        {
            if (request == null || request.PedidoId <= 0)
            {
                _logger.LogWarning("Solicitud de reembolso inválida: PedidoId={PedidoId}, Currency={Currency}",
            request?.PedidoId, request?.Currency);
            }

            try
            {
                var refund = await _paypalService.RefundSaleAsync(request.PedidoId, request.Currency);

                return RedirectToAction("Index", "Pedidos");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar el reembolso: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> RefundPartial([FromBody] RefundRequestModel request)
        {
            if (request?.PedidoId <= 0)
            {
                return Json(new { success = false, message = "Solicitud inválida." });
            }

            try
            {
                await _paypalService.RefundPartialAsync(request.PedidoId, request.Currency,request.Motivo);
               
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult FormularioRembolso()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FormularioRembolso(RefundForm form)
        {
            try
            {
                var (obtenerNumeroPedido,_) = await _policyExecutor.ExecutePolicyAsync(() => _paymentRepository.ObtenerNumeroPedido(form));

                if (obtenerNumeroPedido == null)
                {
                    _logger.LogInformation("El numero de pedido proporcionado no existe " + obtenerNumeroPedido);
                }

                int usuarioActual = _utilityClass.ObtenerUsuarioIdActual();

                var emailCliente = await _policyExecutor.ExecutePolicyAsync(() => _paymentRepository.ObtenerEmailUsuarioAsync(usuarioActual));
                if(emailCliente == null)
                {
                    _logger.LogInformation("El email proporcionado no se encuentra registrado "+ emailCliente);
                }
              

                var (pedido,mensaje) = await _policyExecutor.ExecutePolicyAsync(() =>_paymentRepository.ObtenerNumeroPedido(form));

                if (pedido == null)
                {
                   _logger.LogInformation("El pedido con el numero de pedido proporcionado no existe ");
                }

                var detallespago = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalService.ObtenerDetallesPagoEjecutadoV2(pedido.OrderId));

                if (detallespago == null)
                {
                    _logger.LogInformation("No se ha podido obtener los detalles del pago");
                }

                // Verificar que hay purchase units
                if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
                {
                    _logger.LogInformation("No se encuntran las unidades de pago en la peticion");
                }

                var firstPurchaseUnit = detallespago.PurchaseUnits.First();

                var detallesSuscripcion = _paymentRepository.ProcesarDetallesSuscripcion(detallespago);

                // Lista para almacenar los ítems de PayPal
                var paypalItems = await _paymentRepository.ProcesarRembolso(firstPurchaseUnit, detallesSuscripcion,usuarioActual,form,obtenerNumeroPedido,emailCliente);

                return RedirectToAction("Index", "Admin");
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