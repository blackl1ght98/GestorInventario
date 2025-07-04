using System.Security.Claims;

namespace GestorInventario.Infraestructure.Utils
{
    public class UtilityClass
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public UtilityClass(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public int ObtenerUsuarioIdActual()
        {
            var value = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var id))
            {
                throw new InvalidOperationException("No se pudo obtener el ID del usuario autenticado.");
            }
            return id;
        }
    }
}
