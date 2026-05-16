using GestorInventario.Application.DTOs.Email;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.ViewModels;
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
        private readonly IPolicyExecutor _policyExecutor;      
        private readonly IPaginationHelper _paginationHelper;       
        private readonly ICurrentUserAccessor _currentUserAccessor;     
        private readonly IPedidoManagementService _pedidoService;
        private readonly IPaymentService _paymentService;
        public PedidosController( 
            ILogger<PedidosController> logger, 
            IPaginationHelper pagination,  
            ICurrentUserAccessor currentUser,  
            IPaymentService paymentService,
            IPedidoRepository pedidoRepository,        
            IPolicyExecutor policyExecutor,         
            IPedidoManagementService pedidoService)
        {          
            _logger = logger;
            _pedidoRepository = pedidoRepository;           
        
            _policyExecutor = policyExecutor;
          
            _paginationHelper = pagination;          
            _currentUserAccessor = currentUser;
       
            _pedidoService = pedidoService;
            _paymentService = paymentService;
        }

        [Authorize]
        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var usuarioId = _currentUserAccessor.GetCurrentUserId();
                await _paymentService.LimpiarPedidoCorruptoUsuarioAsync(usuarioId);
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
                var viewmodel = new PedidoDeleteViewmodel
                {
                    Id = pedido.Id,
                    NumeroPedido = pedido.NumeroPedido,
                    FechaPedido = pedido.FechaPedido,
                    NombreCompleto = pedido.IdUsuarioNavigation.NombreCompleto,
                    EstadoPedido = pedido.EstadoPedido,
                    DetallePedidos = pedido.DetallePedidos.ToList(),
                };
                return View(viewmodel);
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
                var success = await _policyExecutor.ExecutePolicyAsync(() => _pedidoService.EliminarPedido(Id));
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
                                     
                var pedido = await _policyExecutor.ExecutePolicyAsync(()=> _pedidoRepository.ObtenerPedidoPorIdAsync(id));
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
                var viewmodel = new DetallePedidoViewmodel
                {
                    FechaPedido= pedido.FechaPedido,
                    NombreCompleto=pedido.IdUsuarioNavigation.NombreCompleto,
                    TrackingNumber=pedido.TrackingNumber,
                    Transportista=pedido.Transportista,
                    NumeroPedido=pedido.NumeroPedido,
                    EstadoPedido=pedido.EstadoPedido,
                    Currency=pedido.Currency,
                    DetallePedidos=pedido.DetallePedidos.ToList(),
                };
                return View(viewmodel);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al obtener los detalles del pedido");
                return RedirectToAction("Error", "Home");
            }

        }

    }
}
