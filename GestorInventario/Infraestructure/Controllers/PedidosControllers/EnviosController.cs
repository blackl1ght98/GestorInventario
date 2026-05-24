using GestorInventario.enums;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.PedidosControllers
{
    public class EnviosController : Controller
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPaypalOrderTrackingService _paypalOrderService;
        private readonly ILogger<EnviosController> _logger;
        private readonly IPaypalService _paypalService;
        public EnviosController(IPedidoRepository pedidoRepository, IPaypalOrderTrackingService paypalOrderService, IPaypalService paypalService, ILogger<EnviosController> logger)
        {
            _pedidoRepository = pedidoRepository;
            _paypalOrderService = paypalOrderService;
            _logger = logger;
            _paypalService=paypalService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AgregarInfoEnvio(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            
            try
            {
                var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
                if (pedido == null)
                {
                    _logger.LogError("Intento de manipulacion de id");
                    return RedirectToAction("Index", "Pedidos");
                }
                var result=  await _paypalOrderService.SeguimientoPedido(pedido.Id, carrier, barcode);
                await _paypalService.AddInfoTrackingOrder(result.Data.pedidoId, result.Data.trackingNumber, result.Data.trackingURL, result.Data.carrier);
                TempData["SuccessMessage"] = "Información de envío agregada con éxito.";
                return RedirectToAction("Index", "Pedidos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar seguimiento para pedido {PedidoId}", pedidoId);

                string userMessage = "No se pudo agregar la información de envío en este momento. " +
                                    "Parece haber un problema temporal con los servidores de PayPal. " +
                                    "Por favor, inténtelo de nuevo más tarde.";

                if (ex.Message.Contains("INTERNAL_SERVICE_ERROR") || ex.Message.Contains("500"))
                {
                    userMessage = "Error temporal en PayPal. Intente nuevamente en unos minutos.";
                }

                TempData["ErrorMessage"] = userMessage;
                return RedirectToAction("Index","Pedidos");
            }
        }
    }
}
