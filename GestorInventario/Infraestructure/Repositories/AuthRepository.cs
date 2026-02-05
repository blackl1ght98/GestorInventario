using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly HashService _hashService;        
        private readonly ILogger<AuthRepository> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ICarritoRepository _carritoRepository;       
        private readonly ICurrentUserAccessor _currentUserAccessor;
        public AuthRepository(GestorInventarioContext context, HashService hash, ICurrentUserAccessor current,
            ILogger<AuthRepository> logger, IUserRepository user, ICarritoRepository carritoRepository)
        {
            _context = context;
            _hashService = hash;
            _logger = logger;
            _userRepository = user;
            _carritoRepository = carritoRepository;          
            _currentUserAccessor = current;
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
                    var usuarioId = _currentUserAccessor.GetCurrentUserId();
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
                var usuario = await _userRepository.ObtenerUsuarioPorId(cambio.UserId);
                if (usuario == null)
                    return OperationResult<Usuario>.Fail("El usuario no existe");

                if (usuario.Data.EmailVerificationToken != cambio.Token)
                    return OperationResult<Usuario>.Fail("El token no es valido");

                if (usuario.Data.FechaEnlaceCambioPass < DateTime.Now || usuario.Data.FechaExpiracionContrasenaTemporal < DateTime.Now)
                {
                    usuario.Data.FechaEnlaceCambioPass = null;
                    usuario.Data.FechaExpiracionContrasenaTemporal = null;
                    usuario.Data.TemporaryPassword = null;
                    await _context.SaveChangesAsync();
                   
                    return OperationResult<Usuario>.Fail("El enlace y contraseña temporal han expirado. Solicite una nueva");
                };
                return OperationResult<Usuario>.Ok("Token valido", usuario.Data);
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
                var usuarioId = _currentUserAccessor.GetCurrentUserId();
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
                _logger.LogError(ex, "Error al eliminar carritos activos para el usuario {UsuarioId}", _currentUserAccessor.GetCurrentUserId());
             
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
