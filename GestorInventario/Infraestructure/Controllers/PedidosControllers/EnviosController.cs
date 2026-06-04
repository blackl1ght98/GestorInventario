using GestorInventario.Application.DTOS.Paypal;

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
                // 1. Leer pedido de TU base de datos (con detalles y captures)
                var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);
                if (pedido == null)
                {
                    _logger.LogError("Intento de manipulacion de id: {PedidoId}", pedidoId);
                    return RedirectToAction("Index", "Pedidos");
                }

                var capture = pedido.PayPalPaymentCaptures?.FirstOrDefault();
                if (capture == null)
                {
                    _logger.LogError("Pedido {PedidoId} no tiene captura de PayPal", pedidoId);
                    TempData["ErrorMessage"] = "El pedido no tiene pago asociado en PayPal.";
                    return RedirectToAction("Index", "Pedidos");
                }

                // 2. Mapear items del pedido a DTOs planos (tu dominio → datos planos)
                var items = pedido.DetallePedidos.Select(d => new TrackingItemDto
                {
                    Name = d.Producto?.NombreProducto ?? "Producto no disponible",
                    Sku = d.Producto?.Descripcion ?? "N/A",
                    Quantity = d.Cantidad ?? 1,
                    BarcodeType = barcode,
                    BarcodeCode = d.Producto?.CodigoBarras ?? "N/A",
                    ImageUrl = d.Producto?.Imagen ?? string.Empty,
                    Url = string.Empty
                }).ToList();

                // 3. Llamar al servicio de PayPal 
                var result = await _paypalOrderService.AddTrackingAsync(
                    payPalOrderId: capture.PaymentId,   // El OrderId de PayPal para la URL
                    captureId: capture.CaptureId,       // El CaptureId para el body
                    carrier: carrier,
                    barcode: barcode,
                    items: items);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Index", "Pedidos");
                }

                // 4. Guardar resultado en base de datos
                await _paypalService.AddInfoTrackingOrder(
                    pedidoId,
                    result.Data.TrackingNumber,
                    result.Data.TrackingUrl,
                    carrier.ToString());

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
                return RedirectToAction("Index", "Pedidos");
            }
        }
    }
}
