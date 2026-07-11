
using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;
using GestorInventario.Infrastructure.Data;
using GestorInventario.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace GestorInventario.Controllers.HomeController
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GestorInventarioContext _context;
        
        public HomeController(ILogger<HomeController> logger, GestorInventarioContext g)
        {
            _logger = logger;
            _context = g;
        
        }

        public IActionResult Index()
        {
           
           
            return View();
        }
        /**    
         DASHBOARD EN PROCESO DE CONTRUCCION
         */
        [Authorize]
        public async Task<IActionResult> DashBoard()
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var hoy = DateTime.Today;

            var pedidosPagadosQuery = _context.Pedidos.Where(p => p.EstadoPedido == EstadoPedido.Pagado.ToString());
            var pedidosEntregadosQuery = _context.Pedidos.Where(p => p.EstadoPedido == EstadoPedido.Entregado.ToString());
            var vm = new DashboardViewModel
            {
                PedidosPagados = await pedidosPagadosQuery.CountAsync(),
                PedidosDevueltos = await _context.Pedidos.CountAsync(p => p.EstadoPedido == EstadoPedido.Rembolsado.ToString()),
                PedidosEntregados = await pedidosEntregadosQuery.CountAsync(),
                PedidosCancelados = await _context.Pedidos.CountAsync(p => p.EstadoPedido == EstadoPedido.Cancelado.ToString()),
                PedidosHoy = await _context.Pedidos.CountAsync(p => p.FechaPedido >= hoy),
                PedidosEsteMes = await _context.Pedidos.CountAsync(p => p.FechaPedido >= inicioMes),
                IngresosTotales = await pedidosEntregadosQuery.SumAsync(p => (decimal?)p.Total) ?? 0,
                IngresosMesActual = await pedidosEntregadosQuery.Where(p => p.FechaPedido >= inicioMes).SumAsync(p => (decimal?)p.Total) ?? 0,
                TicketPromedio = await pedidosEntregadosQuery.AverageAsync(p => (decimal?)p.Total) ?? 0,
                        
            };

            return View(vm);
        }
        public IActionResult Error()
        {
            return View();
        }


    }
}
