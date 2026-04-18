using AutoMapper;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class UserRepository: IUserRepository
    {
       
        private readonly GestorInventarioContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly HashService _hashService;
        private readonly IMapper _mapper;
        public UserRepository( GestorInventarioContext context, ILogger<UserRepository> logger, HashService hash, IMapper map)
        {
         
            _context = context;
            _logger = logger;
            _hashService = hash;
            _mapper = map;

        }
        //Operaciones simples
        public async Task<OperationResult<Usuario>> ObtenerUsuarioPorId(int id)
        {
            var usuario = await _context.Usuarios.AsTracking()
                .Include(x => x.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (usuario == null)
            {
                return OperationResult<Usuario>.Fail("Usuario no encontrado");
            }
            else
                return OperationResult<Usuario>.Ok("Usuario obtenido con exito", usuario);
        }
        //Operaciones complejas
        public async Task<OperationResult<EntityUser>> ObtenerUsuarioParaEdicionAsync(int id)
        {
            var usuarioEf = await _context.Usuarios.AsTracking()
                .Include(x => x.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (usuarioEf == null)
            {
                return OperationResult<EntityUser>.Fail("Usuario no encontrado");
            }

            // Mapeamos de EF → Domain Entity
            var usuarioDominio = _mapper.Map<EntityUser>(usuarioEf);

            return OperationResult<EntityUser>.Ok("Usuario obtenido con éxito", usuarioDominio);
        }
        public IQueryable<Usuario> ObtenerUsuarios()
        {
            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
        }
        public IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId)
        {
            return _context.Usuarios
                .Where(u => u.IdRol == rolId)
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
        }
       
        public async Task<List<Usuario>> ObtenerUsuariosAsync() => await ObtenerUsuarios().ToListAsync();
        public async Task<(Usuario?, string)> ObtenerUsuarioConPedido(int id)
        {
            var usuario = await _context.Usuarios.Include(p => p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
            return usuario is null ? (null, "Este usuario no tiene pedidos") : (usuario, "Usuario con pedidos encontrado");
        }
        public async Task ConfirmEmail(ConfirmRegistrationDto confirm)
        {

            var usuarioUpdate = _context.Usuarios.AsTracking().FirstOrDefault(x => x.Id == confirm.UserId);
            if (usuarioUpdate != null)
            {
                usuarioUpdate.ConfirmacionEmail = true;
                await _context.UpdateEntityAsync(usuarioUpdate);
            }
        }
        public async Task<OperationResult<string>> ActualizarEmailVerificationTokenAsync(int userId, string token)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(userId);
                if (usuario == null)
                {
                    return OperationResult<string>.Fail("Usuario no encontrado");
                }

                usuario.EmailVerificationToken = token;
                await _context.UpdateEntityAsync(usuario);
              

                return OperationResult<string>.Ok("Token actualizado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar EmailVerificationToken del usuario {UserId}", userId);
                return OperationResult<string>.Fail("Error al actualizar el token de verificación");
            }
        }
        public async Task<OperationResult<(string temporaryPassword, string token)>> GenerarYGuardarPasswordTemporalAsync(string email)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuario = await _context.Usuarios
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (usuario == null)
                    return OperationResult<(string, string)>.Fail("Usuario no encontrado");

                // Generar contraseña temporal
                var contrasenaTemporal = GenerarContrasenaTemporal();   

                // Hashear
                var resultadoHash = _hashService.Hash(contrasenaTemporal);

                // Actualizar campos
                usuario.TemporaryPassword = resultadoHash.Hash;
                usuario.Salt = resultadoHash.Salt;
                var fechaExpiracion = DateTime.Now.AddMinutes(5);

                usuario.FechaEnlaceCambioPass = fechaExpiracion;
                usuario.FechaExpiracionContrasenaTemporal = fechaExpiracion;

                await _context.UpdateEntityAsync(usuario);
                return OperationResult<(string, string)>.Ok("Password temporal generada",
                    (contrasenaTemporal, usuario.EmailVerificationToken ?? ""));
            });
           
        }
        private string GenerarContrasenaTemporal()
        {
            var length = 12;
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<List<string>> ObtenerEmailsEmpleadosAsync()
        {
            return await _context.Usuarios
                .Where(u => u.IdRolNavigation.Nombre == "Empleado")
                .Select(u => u.Email)
                .ToListAsync();
        }

    }
}
