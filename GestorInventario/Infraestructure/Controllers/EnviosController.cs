using GestorInventario.enums;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers
{
    public class EnviosController : Controller
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPaypalOrderTrackingService _paypalService;
        private readonly ILogger<EnviosController> _logger;

        public EnviosController(IPedidoRepository pedidoRepository, IPaypalOrderTrackingService paypalService, ILogger<EnviosController> logger)
        {
            _pedidoRepository = pedidoRepository;
            _paypalService = paypalService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AgregarInfoEnvio(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null)
            {
                _logger.LogError("Intento de manipulacion de id");
                return RedirectToAction("Index", "Pedidos");
               
            }

            try
            {
                await _paypalService.SeguimientoPedido(pedido.Id, carrier, barcode);

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
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
