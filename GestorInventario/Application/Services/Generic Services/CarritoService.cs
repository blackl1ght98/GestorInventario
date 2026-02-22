using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class CarritoService : ICarritoService
    {
        private readonly GestorInventarioContext _context;
        private readonly ICarritoRepository _carritoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<CarritoService> _logger;

        public CarritoService(GestorInventarioContext context,ICarritoRepository carritoRepository,
        ICurrentUserAccessor currentUserAccessor,ILogger<CarritoService> logger)
        {
            _context = context;
            _carritoRepository = carritoRepository;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
        }

        public async Task EliminarCarritosActivosAsync()
        {
            try
            {
                var usuarioId = _currentUserAccessor.GetCurrentUserId();

                var carritosActivos = await _context.Pedidos
                    .Where(p => p.IdUsuario == usuarioId && p.EsCarrito)
                    .ToListAsync();

                foreach (var carrito in carritosActivos)
                {
                    var items = await _carritoRepository.ObtenerItemsDelCarritoUsuario(carrito.Id);

                    // Solo eliminamos carritos vacíos
                    if (!items.Data.Any())
                    {
                        _context.Remove(carrito);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar carritos activos para el usuario {UsuarioId}",
                    _currentUserAccessor.GetCurrentUserId());
                throw; // o manejar según tu política
            }
        }
    }
}
