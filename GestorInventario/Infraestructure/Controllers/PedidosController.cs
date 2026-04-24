using GestorInventario.Application.DTOs.Email;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace GestorInventario.Infraestructure.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
       
       
        private readonly ILogger<PedidosController> _logger;
        private readonly IPedidoRepository _pedidoRepository;                 
        private readonly IPdfService _pdfservice;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaypalOrderTrackingService _paypalService;     
        private readonly IPaginationHelper _paginationHelper;       
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IEmailService _emailService;
        private readonly IPaymentRepository _payment;
        private readonly IPedidoManagementService _pedidoService;
        public PedidosController( ILogger<PedidosController> logger, IPaginationHelper pagination,  ICurrentUserAccessor current, IPaymentRepository pay,
            IPedidoRepository pedido,   IPdfService pdf, IPolicyExecutor executor, IPaypalOrderTrackingService paypal, IEmailService email, IPedidoManagementService pedidoService)
        {          
            _logger = logger;
            _pedidoRepository = pedido;           
            _pdfservice= pdf;   
            _policyExecutor = executor;
            _paypalService = paypal;           
            _paginationHelper = pagination;          
            _currentUserAccessor = current;
            _emailService = email;
            _payment = pay;
            _pedidoService = pedidoService;
        }

        [Authorize]
        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin, [FromQuery] Paginacion paginacion)
        {
            try
            {
               

                    var usuarioId =  _currentUserAccessor.GetCurrentUserId();
                await _payment.LimpiarPedidoCorruptoUsuarioAsync(usuarioId);
                    var pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidos());
                    if (User.IsAdministrador())
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidos());
                    }
                    else
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidoUsuario(usuarioId));
                    }
                    ViewData["Buscar"] = buscar;
                    ViewData["FechaInicio"] = fechaInicio;
                    ViewData["FechaFin"] = fechaFin;
                    if (!String.IsNullOrEmpty(buscar))
                    {
                        pedidos = pedidos.Where(p => p.NumeroPedido.Contains(buscar) || p.EstadoPedido.Contains(buscar) || p.IdUsuarioNavigation.NombreCompleto.Contains(buscar));
                    }
                    if (fechaInicio.HasValue && fechaFin.HasValue)
                    {
                        pedidos = pedidos.Where(s => s.FechaPedido >= fechaInicio.Value && s.FechaPedido <= fechaFin.Value);
                    }
                    var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                  _paginationHelper.PaginarAsync(pedidos, paginacion)
                    );
                    var viewModel = new PedidoViewModel
                    {
                        Pedidos = paginationResult.Items,
                        Paginas = paginationResult.Paginas.ToList(),
                        TotalPaginas = paginationResult.TotalPaginas,
                        PaginaActual = paginacion.Pagina,
                        Buscar = buscar
                    };

                 
                    return View(viewModel);
                
               
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los pedidos");
                return RedirectToAction("Error", "Home");
            }
        }
      
       
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                                          
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoConDetallesAsync(id));
            
                if (pedido == null)
                {
                    _logger.LogCritical("Pedido no encontrado");
                    return RedirectToAction(nameof(Index));
                }               
                return View(pedido);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de eliminacion del pedido");
                return RedirectToAction("Error", "Home");
            }

        }

        [Authorize]
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
                            
                    
                    var success = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoService.EliminarPedido(Id)) ;
                    if (success.Success)
                    {
                       
                        
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        
                        TempData["ErrorMessage"] = success.Message;
                      
                        return RedirectToAction(nameof(Delete), new { id = Id });

                    }                         

            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el pedido");
                return RedirectToAction("Error", "Home");
            }

        }
       
        [Authorize]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                                     
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoPorId(id));
                if (pedido == null)
                {
                    _logger.LogError("El pedido no existe");
                    return RedirectToAction(nameof(Index));
                }
                EditPedidoViewModel pedidosViewModel = new EditPedidoViewModel
                {
                    
                    FechaPedido = pedido.FechaPedido,
                    EstadoPedido = pedido.EstadoPedido,

                };
                return View(pedidosViewModel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al mostrar la vista de  editar el pedido");
                return RedirectToAction("Error", "Home");
            }

        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditPedidoViewModel model)
        {
           
            if (ModelState.IsValid)
            {
                try
                {
                    
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoService.EditarPedido(model));
                    if (success.Success)
                    {
                        _logger.LogInformation("Datos actualizados con exito");
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError(success.Message);
                        return RedirectToAction(nameof(Edit));
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error de concurrencia");
                  
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoService.EditarPedido(model));
                    if (success.Success)
                    {
                       
                    }
                    else
                    {
                        _logger.LogError(success.Message);
                        return RedirectToAction(nameof(Edit));
                    }
                
                
                }
                catch (Exception ex)
                {
                    TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                    _logger.LogError(ex, "Error al editar el pedido");
                    return RedirectToAction("Error", "Home");
                }
               
            }
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> DetallesPedido(int id)
        {
            try
            {
                
            
               var pedido= await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoConDetallesAsync(id)) ;
                if (pedido == null)
                {
                    _logger.LogCritical("Pedido no encontrado: no se puede mostrar los detalles de un pedido inexistente");
                    return RedirectToAction(nameof(Index));
                }

                return View(pedido);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los detalles del pedido");
                return RedirectToAction("Error", "Home");
            }

        }

        
       
       
      
       
        
        [Authorize]
        public async Task<IActionResult> DetallesPagoEjecutado(string id)
        {
            var detallepago= await _policyExecutor.ExecutePolicyAsync(()=> _pedidoService.SincronizarDetallePagoAsync(id));
            if (detallepago.Success)
            {
                return View(detallepago.Data);
            }
            else
            {
                _logger.LogError(detallepago.Message);
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpGet]
        public async Task<IActionResult> DownloadInvoice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("Intento de manipulacion de id");
                return RedirectToAction("Error", "Home");
            }

            var resultado = await _pdfservice.GenerarFacturaPagoEjecutadoAsync(id);

            if (!resultado.Success)
            {
                TempData["ErrorMessage"] = resultado.Message ?? "No se pudo generar la factura.";
                return RedirectToAction("DetallesPagoEjecutado", new { id }); 
            }

            var pdfBytes = resultado.Data;
            var fileName = $"Factura_Pago_{id}.pdf";
           
      
            return File(pdfBytes, "application/pdf", fileName);
        }
        [HttpGet]
        [Authorize] // Opcional, según quién pueda enviar facturas
        public async Task<IActionResult> SendInvoiceByEmail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogError("Intento de manipulacion de id");
                return RedirectToAction("Error", "Home");
            }
         
            // Obtener email del cliente       
            var emailDestinatario =  _currentUserAccessor.GetCurrentUserEmail();

            if (string.IsNullOrEmpty(emailDestinatario))
            {
                TempData["ErrorMessage"] = "No se encontró email del cliente.";
                return RedirectToAction("DetallesPagoEjecutado", new { id });
            }

            // Preparar modelo para la plantilla del email
            var emailModel = new FacturaViewmodel
            {
                IdPago = id,
                EnlaceDescarga = Url.Action(nameof(DownloadInvoice), "Pedidos", new { id }, Request.Scheme) 
                                                                                                     
            };
            
            var emailDto = new EmailDto { ToEmail = emailDestinatario };
            await _emailService.SendEmailAsyncFactura(emailDto, id); 
            TempData["SuccessMessage"] = "Factura enviada por email correctamente.";

            return RedirectToAction(nameof(Index));
        }
        [Authorize]
        [HttpPost]
     
        public async Task<IActionResult> AgregarInfoEnvio(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoPorId(pedidoId);
            if (pedido == null)
            {
                _logger.LogError("Intento de manipulacion de id");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _paypalService.SeguimientoPedido(pedido.Id, carrier, barcode);

                TempData["SuccessMessage"] = "Información de envío agregada con éxito.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar seguimiento para pedido {PedidoId}", pedidoId);

                // Mensaje amigable para el usuario (no exponer detalles técnicos)
                string userMessage = "No se pudo agregar la información de envío en este momento. " +
                                    "Parece haber un problema temporal con los servidores de PayPal. " +
                                    "Por favor, inténtelo de nuevo más tarde.";

                // Opcional: si quieres diferenciar errores
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
