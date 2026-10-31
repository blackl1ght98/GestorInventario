using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Pedidos;
using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.MetodosPaginacion;
using GestorInventario.Application.Interfaces.Notifications.InternalNotification;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Domain.enums.Notification;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Extensions;
using GestorInventario.Mappers;
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
        private readonly IOrderRepository _orderRepository;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaginationService _paginationService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;

        public PedidosController(
            ILogger<PedidosController> logger,
            IPaginationService pagination,
            ICurrentUserAccessor currentUser,
            IOrderRepository orderRepository,
            IPolicyExecutor policyExecutor,
            IOrderService orderService,
            INotificationService notificationService
        )
        {
            _logger = logger;
            _orderRepository = orderRepository;
            _policyExecutor = policyExecutor;
            _paginationService = pagination;
            _currentUserAccessor = currentUser;
            _orderService = orderService;
            _notificationService=notificationService;
        }

        [Authorize]
        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin,
            [FromQuery] Paginacion paginacion)
        {
            try
            {
                var usuarioId = _currentUserAccessor.GetCurrentUserId();

                var pedidos = _policyExecutor.ExecutePolicy(() => _orderRepository.ObtenerPedidos());
                if (User.IsAdministrador())
                {
                    pedidos = _policyExecutor.ExecutePolicy(() => _orderRepository.ObtenerPedidos());
                }
                else
                {
                    pedidos = _policyExecutor.ExecutePolicy(() => _orderRepository.ObtenerPedidoUsuario(usuarioId));
                }

                ViewData["Buscar"] = buscar;
                ViewData["FechaInicio"] = fechaInicio;
                ViewData["FechaFin"] = fechaFin;
                if (!string.IsNullOrEmpty(buscar))
                {
                    pedidos = pedidos.Where(p =>
                        p.NumeroPedido.Contains(buscar) || p.EstadoPedido.Contains(buscar) ||
                        p.IdUsuarioNavigation.NombreCompleto.Contains(buscar));
                }

                if (fechaInicio.HasValue && fechaFin.HasValue)
                {
                    pedidos = pedidos.Where(s => s.FechaPedido >= fechaInicio.Value && s.FechaPedido <= fechaFin.Value);
                }

                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationService.PaginarAsync(pedidos, paginacion)
                );
                var pedidosDto = paginationResult.Items.Select(p => new PedidoDtoViewModel
                {
                    Id = p.Id,
                    NumeroPedido = p.NumeroPedido,
                    EstadoPedido = p.EstadoPedido,
                    FechaPedido = p.FechaPedido,
                    NombreCompleto = p.IdUsuarioNavigation?.NombreCompleto,
                    TrackingNumber = p.TrackingNumber,
                    Transportista = p.Transportista,
                    DetallePedidos = p.DetallePedidos.ToList(),
                    PalPaymentCaptures = p.PayPalPaymentCaptures.ToList(),
                    EsCarrito = p.EsCarrito,
                    Currency = p.Currency,
                    Carrier = GetCarrierFromEnum()
                }).ToList();
                var viewModel = new OrderViewModel
                {
                    Pedidos = pedidosDto,
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginacion.Pagina,
                    Carrier = GetCarrierFromEnum(),
                    Buscar = buscar
                };


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los pedidos");
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> DetallesPedido(int id)
        {
            try
            {
                var pedido = await _orderService.GetOrderDetailsAsync(id);
                if (pedido.Success)
                {
                    var result = pedido.Unwrap();
                    var viewmodel = result.ToViewModel();
                    return View(viewmodel);
                }

                if ( pedido.Message.Contains("El carrito"))
                {
                    await _notificationService.CrearNotificacion("Carrito Vacio",
                        "No se puede mostrar el detalle porque el carrito está vacío.", TipoNotificacion.Error);
                  
                }
              
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los detalles del order");
                return RedirectToAction(nameof(Index));
            }
        }

        // Usuario o Admin cancela antes de envío
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelarPedido([FromQuery] int pedidoId)
        {
            var pedido = await _orderRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null) return NotFound();

            // Solo se puede cancelar si no está enviado ni entregado
            if (pedido.EstadoPedido == EstadoPedido.Enviado.ToString()
                || pedido.EstadoPedido == EstadoPedido.Entregado.ToString())
                return BadRequest(new { success = false, errorMessage = "No se puede cancelar un pedido ya enviado" });
            if (pedido.EsCarrito)
            {
                return BadRequest(new {success=false,errorMessage = "No se puede cancelar un pedido que esta en el carrito"});
            }
            pedido.EstadoPedido = EstadoPedido.Cancelado.ToString();
            await _orderRepository.ActualizarPedidoAsync(pedido);

            return Ok(new { success = true, message = "Pedido cancelado correctamente" });
        }

        // Usuario confirma que recibió (o tracking automático)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmarEntrega(int pedidoId)
        {
            var pedido = await _orderRepository.ObtenerPedidoPorIdAsync(pedidoId);
            if (pedido == null) return NotFound();

            if (pedido.EstadoPedido != EstadoPedido.Enviado.ToString())
                return BadRequest("El pedido no está enviado");
            if (pedido.EsCarrito)
            {
                return BadRequest(new {success=false,errorMessage = "No se puede cancelar un pedido que esta en el carrito"});
            }

            if (pedido.EstadoPedido == EstadoPedido.Cancelado.ToString())
            {
                return BadRequest(new {success=false,errorMessage = "No puedes confirmar la entregaa de un pedido cancelado"});
            }
            pedido.EstadoPedido = EstadoPedido.Entregado.ToString();
            await _orderRepository.ActualizarPedidoAsync(pedido);

            return Ok();
        }

        public List<string> GetCarrierFromEnum()
        {
            return Enum.GetNames(typeof(Carrier)).ToList();
        }
    }
}