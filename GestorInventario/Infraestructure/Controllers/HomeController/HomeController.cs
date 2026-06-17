using GestorInventario.Application.Services.Notifications;
using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;
using GestorInventario.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace GestorInventario.Infraestructure.Controllers.HomeController
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
        [Authorize]
        public async Task<IActionResult> DashBoard()
        {
            var vm = new DashboardViewModel
            {
                PedidosPagados = await _context.Pedidos
                    .CountAsync(p => p.EstadoPedido == "Pagado"),
                PedidosDevueltos = await _context.Pedidos.CountAsync(p => p.EstadoPedido == EstadoPedido.Rembolsado.ToString())

            };

            return View(vm);
        }
        public IActionResult Error()
        {
            return View();
        }


    }
}
