using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository: ICarritoRepository
    {
        private readonly GestorInventarioContext _context;

        public CarritoRepository(GestorInventarioContext context)
        {
            _context = context;
        }
        public async Task<Carrito> ObtenerCarrito(int userId)
        {
            return await _context.Carritos.FirstOrDefaultAsync(x => x.UsuarioId == userId);
        }
        public async Task<List<ItemsDelCarrito>> ObtenerItemsCarrito(int userIdcarrito)
        {
            return await _context.ItemsDelCarritos
                        .Include(i => i.Producto)

                         .Include(i => i.Producto.IdProveedorNavigation)

                        .Where(i => i.CarritoId == userIdcarrito)
                        .ToListAsync();
        }
        public async Task<List<ItemsDelCarrito>> ConvertirItemsAPedido(int userIdcarrito)
        {
            return await _context.ItemsDelCarritos
                        .Where(i => i.CarritoId == userIdcarrito)
                        .ToListAsync();
        }
        public async Task<ItemsDelCarrito> ItemsDelCarrito(int Id)
        {
            return await _context.ItemsDelCarritos.FirstOrDefaultAsync(x => x.Id == Id);
        }
        public async Task<Producto> Decrementar(int? id)
        {
            return await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
