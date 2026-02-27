using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels.order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
       
       
        private readonly ILogger<PedidosController> _logger;
        private readonly IPedidoRepository _pedidoRepository;                 
        private readonly IPdfService _pdfservice;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaypalService _paypalService;
        private readonly GestorInventarioContext _context;
        private readonly IPaginationHelper _paginationHelper;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IProductoRepository _productoRepository;
        public PedidosController( ILogger<PedidosController> logger, IPaginationHelper pagination, IUserRepository user, ICurrentUserAccessor current,
            IPedidoRepository pedido,   IPdfService pdf, IPolicyExecutor executor, IPaypalService paypal, GestorInventarioContext context, IProductoRepository producto)
        {          
            _logger = logger;
            _pedidoRepository = pedido;           
            _pdfservice= pdf;   
            _policyExecutor = executor;
            _paypalService = paypal;
            _context = context;
            _paginationHelper = pagination;
            _userRepository = user;
            _currentUserAccessor = current;
            _productoRepository = producto;
        }

        [Authorize]
        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin, [FromQuery] Paginacion paginacion)
        {
            try
            {
               

                var usuarioId =  _currentUserAccessor.GetCurrentUserId();
                    var pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerPedidos());
                    if (User.IsInRole("administrador"))
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
      
        //Metodo que obtiene la informacion necesaria para eliminar un pedido
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                                          
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoEliminacion(id));
            
                if (pedido == null)
                {
                    TempData["ErrorMessage"] = "Pedido no encontrado";
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
        public async Task<IActionResult> DeleteConfirmed(int Id)
        {
            try
            {
               
                
                    
                    var success = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.EliminarPedido(Id)) ;
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
        public async Task<IActionResult> DeleteHistorial(int id)
        {
            try
            {
               
                var historialProducto = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.EliminarHistorialPorId(id));
                if (historialProducto == null)
                {

                    TempData["ErrorMessage"] = "Historial no encontrado";
                   
                }
                return View(historialProducto);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }

        }
        [Authorize]
        [HttpPost, ActionName("DeleteConfirmedHistorial")]
        public async Task<IActionResult> DeleteConfirmedHistorial(int Id)
        {
            try
            {
                
                
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EliminarHistorialPorIdDefinitivo(Id));
                    if (success.Success)
                    {

                        return RedirectToAction(nameof(HistorialPedidos));
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
                _logger.LogError(ex, "Error al eliminar el producto");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize]
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                                     
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoPorId(id)) ;
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
        public async Task<ActionResult> Edit(EditPedidoViewModel model)
        {
           
            if (ModelState.IsValid)
            {
                try
                {
                    
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EditarPedido(model));
                    if (success.Success)
                    {
                        _logger.LogInformation("Datos actualizados con exito");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Error de concurrencia");
                  
                    var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EditarPedido(model));
                    if (success.Success)
                    {
                       
                    }
                    else
                    {
                        TempData["ErrorMessage"] = success.Message;
                    }
                
                
                }
                catch (Exception ex)
                {
                    TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                    _logger.LogError(ex, "Error al editar el pedido");
                    return RedirectToAction("Error", "Home");
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> DetallesPedido(int id)
        {
            try
            {
                
            
               var pedido= await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerDetallesPedido(id)) ;
                if (pedido == null)
                {
                    return NotFound();
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

        
        public async Task<IActionResult> HistorialPedidos(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {
                

                var usuarioId = _currentUserAccessor.GetCurrentUserId();
                    var pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerHistorialDePedidos());
                    if (User.IsInRole("administrador") || pedidos.Count() <0)
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerHistorialDePedidos());
                    }
                    else
                    {

                        pedidos = _policyExecutor.ExecutePolicy(() => _pedidoRepository.ObtenerHistorialDePedidos(usuarioId));
                    }
                    ViewData["Buscar"] = buscar;
                    var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                 _paginationHelper.PaginarAsync(pedidos, paginacion));
                    var viewModel = new HistorialPedidoViewModel
                    {
                        HistorialPedidos = paginationResult.Items,
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
                _logger.LogError(ex, "Error al obtener el historial de pedidos");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize]
        public async Task<IActionResult> DetallesHistorialPedido(int id)
        {
            try
            {
                
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.DetallesHistorial(id)) ;
                if (pedido == null)
                {
                    TempData["ErrorMessage"] = "detalle del historial del pedido no encontrado";
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
        [HttpGet("descargarhistorialpedidoPDF")]
        public async Task<IActionResult> DescargarHistorialPDF()
        {
            try
            {
               

                var  bytes = await _policyExecutor.ExecutePolicyAsync(() => _pdfservice.GenerarPDF());
                if (!bytes.Success)
                {
                    TempData["ErrorMessage"] = bytes.Message;
                    return RedirectToAction(nameof(HistorialPedidos));
                }
                return File(bytes.Data, "application/pdf", "historial.pdf");
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al descargar el pdf");
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo para eliminar el historial
        [Authorize]
        [HttpPost, ActionName("DeleteAllHistorial")]
        public async Task<IActionResult> DeleteAllHistorial()
        {
            try
            {
               

                var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.EliminarHitorial());
                if (!success.Success)
                {
                    TempData["ErrorMessage"] = success.Message;
                    return RedirectToAction(nameof(HistorialPedidos));
                }
                return RedirectToAction(nameof(HistorialPedidos));
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al eliminar los datos del historial");
                return RedirectToAction("Error", "Home");
            }
        }
        
        [Authorize]
        public async Task<IActionResult> DetallesPagoEjecutado(string id)
        {
            var detallepago= await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerDetallePagoEjecutadoV2(id));
            if (detallepago.Success)
            {
                return View(detallepago.Data);
            }
            else
            {
                TempData["ErrorMessage"]=detallepago.Message;
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AgregarInfoEnvio(int pedidoId, Carrier carrier, BarcodeType barcode)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            await _paypalService.SeguimientoPedido(pedido.Id,carrier, barcode);
            return RedirectToAction(nameof(Index), new {message="Info Agregada con exito"});
        }
       
    }
}
