using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Infrastructure.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Controllers.PedidosControllers
{
    public class EnviosController : Controller
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPaypalOrderTrackingService _paypalOrderService;
        private readonly ILogger<EnviosController> _logger;     
        private readonly IOrderService _pedidoService;
        public EnviosController(IPedidoRepository pedidoRepository, IPaypalOrderTrackingService paypalOrderService,  ILogger<EnviosController> logger, IOrderService pedido)
        {
            _pedidoRepository = pedidoRepository;
            _paypalOrderService = paypalOrderService;
            _logger = logger;
            _pedidoService = pedido;
        }  
        [Authorize]
        [HttpPost]
      
        public async Task<IActionResult> AgregarInfoEnvio([FromBody] InfoEnvioDto envio)
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

                // 2. Transformar la VERDAD de la BD al formato de PayPal
                var paypalItems = pedido.DetallePedidos
                    .Select(d => d.ToDto())
                    .ToList();
    
                // 3. Ejecutar la acción con datos verificados
                var result = await _paypalOrderService.AddTrackingAsync(
                    capture.PaymentId,
                    capture.CaptureId,
                    envio.Carrier,
                    paypalItems);

                if (!result.Success)
                {
                    
                    return BadRequest(new {success=false,message=result.Message});
                }

                // 4. Guardar resultado en base de datos
                await _pedidoService.AddInfoTrackingOrder(
                    envio.PedidoId,
                    result.Data,//->  numero de seguimiento
                 
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
