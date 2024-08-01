using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly HashService _hashService;
        private readonly IHttpContextAccessor _contextAccessor;
        public AuthRepository(GestorInventarioContext context, HashService hash, IHttpContextAccessor httpcontextAccessor)
        {
            _context = context;
            _hashService = hash;
            _contextAccessor = httpcontextAccessor;
        }

        public async Task<Usuario> Login(string email)
        {
            return await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<Usuario> ExisteEmail(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        }
        public async Task<Usuario> ObtenerPorId(int id)
        {
            //Alternativa --> _context.Usuarios.ExistUserId(confirmar.UserId) esto con metodo de extension
            return await _context.Usuarios.FindAsync(id);
        }
        public async Task<(bool, string)> RestorePass(DTORestorePass cambio)
        {
            var usuarioDB = await ObtenerPorId(cambio.UserId);
            if (usuarioDB == null)
            {
                return (false, "Usuario no encontrado");
            }
            if (usuarioDB.EnlaceCambioPass != cambio.Token)
            {
                return (false, "Token no valido");
            }
            if (usuarioDB.FechaEnlaceCambioPass < DateTime.Now && usuarioDB.FechaExpiracionContrasenaTemporal < DateTime.Now)
            {
                usuarioDB.FechaEnlaceCambioPass = null;
                usuarioDB.FechaExpiracionContrasenaTemporal = null;
                usuarioDB.TemporaryPassword = null;
                await _context.SaveChangesAsync();
                return (false, "El enlace y contraseña temporal ha expirado, solicite otro");

            }


            return (true, null);
        }
        public async Task<(bool, string)> ActualizarPass(DTORestorePass cambio)
        {
            var usuarioDB = await ObtenerPorId(cambio.UserId);
            if (usuarioDB == null)
            {
                return (false, "Usuario no encontrado");
            }
            if (string.IsNullOrEmpty(cambio.Password))
            {
                return (false, "La contraseña no puede estar vacia");
            }
            // Usa el salt almacenado en la base de datos para generar un hash para la contraseña temporal proporcionada por el usuario
            var resultadoHashTemp = _hashService.Hash(cambio.TemporaryPassword, usuarioDB.Salt);
            if (usuarioDB.TemporaryPassword != resultadoHashTemp.Hash)
            {
                return (false, "La contraseña temporal no es valida");
            
            }
            var resultadoHash = _hashService.Hash(cambio.Password);
            usuarioDB.Password = resultadoHash.Hash;
            usuarioDB.Salt = resultadoHash.Salt;

            _context.UpdateEntity(usuarioDB);
            return (true, null);    
        }
        public async Task<(bool, string)> ChangePassword(string passwordAnterior, string passwordActual)
        {
            var existeUsuario = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int usuarioId;
            if (int.TryParse(existeUsuario, out usuarioId))
            {
                var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId);
                if (usuarioDB == null)
                {
                    return (false,"Usuario no encontrado");
                }
                var anteriorpass = _hashService.Hash(passwordAnterior, usuarioDB.Salt);
                if (usuarioDB.Password != anteriorpass.Hash)
                {
                    return (false,"Contraseña anterior incorrecta");
                }
                var actualPassword = _hashService.Hash(passwordActual);
                usuarioDB.Password = actualPassword.Hash;
                usuarioDB.Salt = actualPassword.Salt;
                _context.UpdateEntity(usuarioDB);
               return(true, null);
            }
            return (false, null);
        }
    }
}
