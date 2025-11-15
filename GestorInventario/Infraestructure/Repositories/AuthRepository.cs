using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly HashService _hashService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<AuthRepository> _logger;
        private readonly UtilityClass _utilityClass;
        private readonly ICarritoRepository _carritoRepository;
        private readonly IAdminRepository _adminRepository;
        public AuthRepository(GestorInventarioContext context, HashService hash, IHttpContextAccessor httpcontextAccessor, 
            ILogger<AuthRepository> logger, UtilityClass utilityClass, ICarritoRepository carritoRepository, IAdminRepository adminRepository)
        {
            _context = context;
            _hashService = hash;
            _contextAccessor = httpcontextAccessor;
            _logger = logger;
            _utilityClass = utilityClass;
            _carritoRepository = carritoRepository;
            _adminRepository = adminRepository;
        }

        public async Task<(Usuario?, string)> Login(string email)
        {
            var login = await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == email);
            return login is null ? (null, "Email no encontrado") : (login, "Login exitoso");
        }
      
        public async Task<OperationResult<string>> ValidateResetTokenAsync(RestoresPasswordDto cambio)
        {
            try
            {
                var resultado = await ValidarTokenCambioPass(cambio);
                return OperationResult<string>.Ok("Validacion exitosa");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restaurar la contraseña");
                return OperationResult<string>.Fail("Ocurrió un error inesperado. Por favor, contacte al administrador o intente nuevamente.");
                
            }
        }


        public async Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var resultadoValidacion = await ValidarTokenCambioPass(cambio);

                if (!resultadoValidacion.Success)
                    return OperationResult<string>.Fail(resultadoValidacion.Message);

                var usuarioDB = resultadoValidacion.Data;

                if (usuarioDB == null)
                    return OperationResult<string>.Fail("Usuario no encontrado");

                if (string.IsNullOrEmpty(cambio.Password))
                    return OperationResult<string>.Fail("La contraseña no puede estar vacía");

                var resultadoHashTemp = _hashService.Hash(cambio.TemporaryPassword, usuarioDB.Salt);
                if (usuarioDB.TemporaryPassword != resultadoHashTemp.Hash)
                    return OperationResult<string>.Fail("La contraseña temporal no es válida");

                var resultadoHash = _hashService.Hash(cambio.Password);
                usuarioDB.Password = resultadoHash.Hash;
                usuarioDB.Salt = resultadoHash.Salt;

                await _context.UpdateEntityAsync(usuarioDB);
                await transaction.CommitAsync();

                return OperationResult<string>.Ok("Contraseña cambiada con exito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la contraseña");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Ocurrió un error inesperado. Por favor, contacte al administrador o intente nuevamente.");
            }
        }


        public async Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                    var usuarioId = _utilityClass.ObtenerUsuarioIdActual();
                    var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId);
                    if (usuarioDB == null)
                    {
                        return OperationResult<string>.Fail("Usuario no encontrado");
                    }
                    var anteriorpass = _hashService.Hash(passwordAnterior, usuarioDB.Salt);
                    if (usuarioDB.Password != anteriorpass.Hash)
                    {
                        return OperationResult<string>.Fail("Contraseña anterior incorrecta");
                    }
                    var actualPassword = _hashService.Hash(passwordActual);
                    usuarioDB.Password = actualPassword.Hash;
                    usuarioDB.Salt = actualPassword.Salt;
                    await _context.UpdateEntityAsync(usuarioDB);
                    await transaction.CommitAsync();
                    return OperationResult<string>.Ok("Contraseña cambiada con exito");                             
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar la contraseña");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
                
            }
           
        }
        private async Task<OperationResult<Usuario>> ValidarTokenCambioPass(RestoresPasswordDto cambio)
        {
            
            try
            {
                var (usuario,mensaje) = await _adminRepository.ObtenerPorId(cambio.UserId);
                if (usuario == null)
                    return OperationResult<Usuario>.Fail("El usuario no existe");

                if (usuario.EnlaceCambioPass != cambio.Token)
                    return OperationResult<Usuario>.Fail("El token no es valido");

                if (usuario.FechaEnlaceCambioPass < DateTime.Now || usuario.FechaExpiracionContrasenaTemporal < DateTime.Now)
                {
                    usuario.FechaEnlaceCambioPass = null;
                    usuario.FechaExpiracionContrasenaTemporal = null;
                    usuario.TemporaryPassword = null;
                    await _context.SaveChangesAsync();
                   
                    return OperationResult<Usuario>.Fail("El enlace y contraseña temporal han expirado. Solicite una nueva");
                };
                return OperationResult<Usuario>.Ok("Token valido", usuario);
            }
            catch (Exception ex) {
                _logger.LogCritical(ex, "Error al validar el token");
                return OperationResult<Usuario>.Fail("Ocurrió un error inesperado al validar el token.");
            
            
            }
           
        }
        public async Task EliminarCarritoActivo()
        {
           
            try
            {
                var usuarioId = _utilityClass.ObtenerUsuarioIdActual();
                var carritosActivos = await _context.Pedidos
                    .Where(p => p.IdUsuario == usuarioId && p.EsCarrito)
                    .ToListAsync();

                foreach (var carritoActivo in carritosActivos)
                {
                    var itemsCarrito = await _carritoRepository.ObtenerItemsDelCarritoUsuario(carritoActivo.Id);
                    if (!itemsCarrito.Any()) // Solo eliminar carritos vacíos
                    {
                      await  _context.DeleteEntityAsync(carritoActivo);
                    }
                }

                await _context.SaveChangesAsync();
          
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar carritos activos para el usuario {UsuarioId}", _utilityClass.ObtenerUsuarioIdActual());
             
            }
        }
        public async Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token)
        {
            try
            {
                var cambio = new RestoresPasswordDto
                {
                    UserId = userId,
                    Token = token
                };

                var resultado = await ValidateResetTokenAsync(cambio);
                if (!resultado.Success)
                    return OperationResult<RestoresPasswordDto>.Fail(resultado.Message);

                return OperationResult<RestoresPasswordDto>.Ok("Modelo preparado con éxito", cambio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el token de restauración de contraseña");
                return OperationResult<RestoresPasswordDto>.Fail("El servidor tardó mucho en responder, inténtelo de nuevo más tarde");
            }
        }
    }
}
