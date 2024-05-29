using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository:IPedidoRepository
    {
        private readonly GestorInventarioContext _context;

        public PedidoRepository(GestorInventarioContext context)
        {
            _context = context;
        }
        public IQueryable<Pedido> ObtenerPedidos()
        {
            var pedidos= from p in _context.Pedidos.Include(dp=>dp.DetallePedidos).ThenInclude(p=>p.Producto).Include(u=>u.IdUsuarioNavigation)
                         select p;
            return pedidos;
        }
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)
        {
            var pedidos= _context.Pedidos.Where(p => p.IdUsuario == userId)
                            .Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto)
                            .Include(u => u.IdUsuarioNavigation);
            return pedidos;
        }
    }
}
