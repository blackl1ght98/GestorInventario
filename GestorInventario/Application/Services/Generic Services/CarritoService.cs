
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;


namespace GestorInventario.Application.Services.Generic_Services
{
    public class CarritoService : ICarritoService
    {
        
        private readonly ICarritoRepository _carritoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<CarritoService> _logger;

        public CarritoService(ICarritoRepository carritoRepository,
        ICurrentUserAccessor currentUserAccessor,ILogger<CarritoService> logger)
        {
          
            _carritoRepository = carritoRepository;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
        }

        public async Task EliminarCarritoActivoAsync()  
        {
            var usuarioId = _currentUserAccessor.GetCurrentUserId();

            if (usuarioId <= 0)  
            {
                _logger.LogWarning("Intento de eliminar carrito sin usuario autenticado");
                return;
            }

            try
            {
                // 1. Obtener el carrito activo del usuario (debería ser solo uno)
                var carritoActivo = await _carritoRepository.ObtenerCarritoUsuario(usuarioId);

                if (carritoActivo.Data == null)
                {
                    _logger.LogDebug("No se encontró carrito activo para el usuario {UsuarioId}", usuarioId);
                    return;
                }

                // 2. Verificar si tiene items
                var items = await _carritoRepository.ObtenerItemsDelCarritoUsuario(carritoActivo.Data.Id);

                if (items?.Data?.Any() == true)
                {
                    _logger.LogInformation("Carrito activo del usuario {UsuarioId} tiene items → no se elimina", usuarioId);
                    return;
                }

                // 3. Eliminar (delegado al repositorio)
                await _carritoRepository.EliminarCarritoAsync(carritoActivo.Data.Id);

                _logger.LogInformation("Carrito activo vacío eliminado para el usuario {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar eliminar el carrito activo del usuario {UsuarioId}", usuarioId);
                return;
            }
        }
    }
}
