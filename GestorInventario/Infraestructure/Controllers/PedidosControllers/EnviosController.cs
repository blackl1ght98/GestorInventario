using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Order;
using GestorInventario.enums.Pedido;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
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
        private readonly IPedidoManagementService _pedidoService;
        public EnviosController(IPedidoRepository pedidoRepository, IPaypalOrderTrackingService paypalOrderService,  ILogger<EnviosController> logger, IPedidoManagementService pedido)
        {
            _pedidoRepository = pedidoRepository;
            _paypalOrderService = paypalOrderService;
            _logger = logger;
            _pedidoService = pedido;
        }  
        [Authorize]
        [HttpPost]
      
        public async Task<IActionResult> AgregarInfoEnvio([FromBody] InfoEnvioDTO envio)
        {
            try
            {
                
                // 1. Leer pedido de TU base de datos (con detalles y captures)
                var pedido = await _pedidoRepository.ObtenerPedidoConCapturasAsync(envio.PedidoId);
                if (pedido == null)
                {
                    _logger.LogError("Intento de manipulacion de id: {PedidoId}", envio.PedidoId);
                    return BadRequest(new {success=false, message="El pedido no es valido"});
                }
                if (pedido.EstadoPedido == EstadoPedido.Cancelado.ToString())
                {
                 
                    return BadRequest(new {success=false, message= "El pedido ha sido cancelado no se puede establecer informacion de envio." });
                }
                var capture = pedido.PayPalPaymentCaptures?.FirstOrDefault();
                if (capture == null)
                {
                    _logger.LogError("Pedido {PedidoId} no tiene captura de PayPal", envio.PedidoId);
                 
                    return BadRequest(new {success=true, message= "El pedido no tiene pago asociado en PayPal" });
                }

                // 2. Mapear items del pedido a DTOs planos (tu dominio → datos planos)
                var items = pedido.DetallePedidos.Select(d => new TrackingItemDto
                {
                    Name = d.Producto?.NombreProducto ?? "Producto no disponible",
                    Sku = d.Producto?.Descripcion ?? "N/A",
                    Quantity = d.Cantidad,
                    BarcodeType = envio.Barcode,
                    BarcodeCode = d.Producto?.CodigoBarras ?? "N/A",
                    ImageUrl = d.Producto?.Imagen ?? string.Empty,
                    Url = string.Empty
                }).ToList();

                // 3. Llamar al servicio de PayPal 
                var result = await _paypalOrderService.AddTrackingAsync(
                    payPalOrderId: capture.PaymentId,   // El OrderId de PayPal para la URL
                    captureId: capture.CaptureId,       // El CaptureId para el body
                    carrier: envio.Carrier,
                    barcode: envio.Barcode,
                    items: items);

                if (!result.Success)
                {
                    
                    return BadRequest(new {success=false,message=result.Message});
                }

                // 4. Guardar resultado en base de datos
                await _pedidoService.AddInfoTrackingOrder(
                    envio.PedidoId,
                    result.Data.TrackingNumber,
                    result.Data.TrackingUrl,
                    envio.Carrier.ToString());

                
                return Ok(new {success=true,message= "Información de envío agregada con éxito." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar seguimiento para pedido {PedidoId}", envio.PedidoId);

                string userMessage = "No se pudo agregar la información de envío en este momento. " +
                                     "Parece haber un problema temporal con los servidores de PayPal. " +
                                     "Por favor, inténtelo de nuevo más tarde.";

                if (ex.Message.Contains("INTERNAL_SERVICE_ERROR") || ex.Message.Contains("500"))
                {
                    userMessage = "Error temporal en PayPal. Intente nuevamente en unos minutos.";
                }

            
                return BadRequest(new { success=false, message=userMessage });
            }
        }
    }
}
