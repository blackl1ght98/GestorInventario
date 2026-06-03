using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



namespace GestorInventario.Infraestructure.Controllers.PaypalControllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaypalOrderService _paypalOrderService;  
        private readonly IPolicyExecutor _policyExecutor;     
        private readonly ICurrentUserAccessor _currentUserAccessor;     
        private readonly IPedidoManagementService _pedidoService;
        private readonly IPaymentService _paymentService;
      
     
        public PaymentController(
            ILogger<PaymentController> logger,   
            ICurrentUserAccessor currentUser,       
            IPolicyExecutor policyExecutor, 
            IPaypalOrderService paypalOrderService,     
            IPedidoManagementService pedidoService, 
            IPaymentService paymentService
          
           )
        {
            _logger = logger;           
            _policyExecutor = policyExecutor;
            _paypalOrderService = paypalOrderService;             
            _currentUserAccessor = currentUser;  
            _pedidoService = pedidoService;
            _paymentService = paymentService;
            
           
           
        }
        
        public async Task<IActionResult> Success(string token=null)
        {
            try
            {
                
                var orderId = token ?? string.Empty;
                //CaptureId->representa el id del pago en paypal
                //total-> lo que has pagado
                //currency-> la moneda
                
               var (captureId, total, currency) = await _paypalOrderService.CapturarPagoAsync(orderId);
                

               var usuarioActual = _currentUserAccessor.GetCurrentUserId();
 
               var result = await _pedidoService.ConfirmarPagoDelPedidoAsync(usuarioActual,captureId,total,currency,orderId);
                if (result.Success)
                {
                    return RedirectToAction("Index", "Pedidos");
                }
                else
                {
                    return RedirectToAction("Error", "Home");
                }
                    
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }
      
        public async Task<IActionResult> ReintentarPago(int pedidoId)
        {
            CultureHelper.SetInvariantCulture();
            try
            {
            

                var resultado = await _policyExecutor.ExecutePolicyAsync(() => _paymentService.ReintentarPago(pedidoId));
              
                if (resultado.Success)
                {
                    return Redirect(resultado.Data);
                }
                else
                {
                    _logger.LogError("Ocurrio un error al redireccionar a paypal");
                    return RedirectToAction("Index", "Home");
                }

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el checkout");
                return RedirectToAction("Error", "Home");
            }

        }
        [Authorize]
        public async Task<IActionResult> Cancel()
        {

            /**
             * DESCOMENTAR LOGICA DE ELIMINACION EN CASO DE NO QUERER LA LOGICA DE REINTENTO DE PAGO Y COMENTAR LA LOGICA DE REINTENTO DE PAGO
             var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            // Buscar el pedido más reciente del usuario que esté sin pagar/corrupto
            var pedidoCorrupto = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                .Where(p => p.IdUsuario == userId)
                .Where(p => p.EstadoPedido == "Pendiente" || p.PayPalPaymentCaptures.Count == 0)
                .OrderByDescending(p => p.FechaPedido)
                .FirstOrDefaultAsync();

            if (pedidoCorrupto != null)
            {

                _context.DetallePedidos.RemoveRange(pedidoCorrupto.DetallePedidos);
                _context.Pedidos.Remove(pedidoCorrupto);
                await _context.SaveChangesAsync();
            }
             
             */
            return RedirectToAction("Index", "Productos");
        }
       
       
    }      
}