using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Extensions;
using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Orders;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;




namespace GestorInventario.Controllers.PedidosControllers
{
    [Authorize]
    public class PedidosController : Controller
    {
            
        private readonly ILogger<PedidosController> _logger;
        private readonly IPedidoRepository _pedidoRepository;                   
        private readonly IPolicyExecutor _policyExecutor;      
        private readonly IPaginationHelper _paginationHelper;       
        private readonly ICurrentUserAccessor _currentUserAccessor;     
       
        
        public PedidosController( 
            ILogger<PedidosController> logger, 
            IPaginationHelper pagination,  
            ICurrentUserAccessor currentUser,  
            IPedidoRepository pedidoRepository,        
            IPolicyExecutor policyExecutor       
          )
        {          
            _logger = logger;
            _pedidoRepository = pedidoRepository;               
            _policyExecutor = policyExecutor;        
            _paginationHelper = pagination;          
            _currentUserAccessor = currentUser;      
           
       
        }

        [Authorize]
        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin, [FromQuery] Paginacion paginacion)
        {
            try
            {
                var usuarioId = _currentUserAccessor.GetCurrentUserId();
           
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
                if (!string.IsNullOrEmpty(buscar))
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
                var viewModel = new OrderViewModel
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
                var viewmodel = new OrderDetailsViewmodel
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
        // Usuario o Admin cancela antes de envío
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelarPedido([FromQuery] int pedidoId)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null) return NotFound();

            // Solo se puede cancelar si no está enviado ni entregado
            if (pedido.EstadoPedido == EstadoPedido.Enviado.ToString()
                || pedido.EstadoPedido == EstadoPedido.Entregado.ToString())
                 return BadRequest(new { success = false, errorMessage = "No se puede cancelar un pedido ya enviado" });

            pedido.EstadoPedido = EstadoPedido.Cancelado.ToString();
            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            return Ok(new {success = true, message="Pedido cancelado correctamente"});
        }

        // Usuario confirma que recibió (o tracking automático)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmarEntrega(int pedidoId)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null) return NotFound();

            if (pedido.EstadoPedido != EstadoPedido.Enviado.ToString())
                return BadRequest("El pedido no está enviado");

            pedido.EstadoPedido = EstadoPedido.Entregado.ToString();
            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            return Ok();
        }
    }
}
