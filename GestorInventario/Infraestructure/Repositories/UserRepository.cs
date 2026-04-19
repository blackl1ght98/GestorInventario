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
        private readonly IMapper _mapper;
        public UserRepository( GestorInventarioContext context, ILogger<UserRepository> logger, IMapper map)
        {
         
            _context = context;
            _logger = logger;
            _mapper = map;

        }
        // Devuelve entidad EF - usar para operaciones de persistencia
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
        // Devuelve entidad de dominio mapeada - usar para lógica de negocio y edición
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
        public async Task<List<Usuario>> ObtenerUsuariosAsync() =>await _context.Usuarios.Include(u => u.IdRolNavigation).ToListAsync();
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
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return OperationResult<string>.Fail("Usuario no encontrado");

            usuario.EmailVerificationToken = token;
            await _context.UpdateEntityAsync(usuario);
            return OperationResult<string>.Ok("Token actualizado correctamente");
        }
        public async Task<OperationResult<Usuario>> GuardarPasswordTemporalAsync(
        string email, string hash, byte[] salt, DateTime fechaExpiracion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuario = await _context.Usuarios
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Email == email);

                if (usuario == null)
                    return OperationResult<Usuario>.Fail("Usuario no encontrado");

                usuario.TemporaryPassword = hash;
                usuario.Salt = salt;
                usuario.FechaEnlaceCambioPass = fechaExpiracion;
                usuario.FechaExpiracionContrasenaTemporal = fechaExpiracion;

                await _context.UpdateEntityAsync(usuario);
                return OperationResult<Usuario>.Ok("Password temporal guardada", usuario);
            });
        }
        public async Task<List<string>> ObtenerEmailsEmpleadosAsync()
        {
            return await _context.Usuarios
                .Where(u => u.IdRolNavigation.Nombre == "Empleado")
                .Select(u => u.Email)
                .ToListAsync();
        }
        /**
        Metodo llamado en el servicio UserManagementService.
        */
        public async Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(usuario);
                return OperationResult<Usuario>.Ok("Usuario guardado", usuario);
            });
        }
        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(x => x.Email == email);
        }
        public async Task<OperationResult<string>> ActualizarUsuarioAsync(EntityUser usuarioDominio)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuarioEf = await _context.Usuarios.FindAsync(usuarioDominio.Id);
                if (usuarioEf == null)
                    throw new InvalidOperationException($"Usuario con ID {usuarioDominio.Id} no encontrado");

                _mapper.Map(usuarioDominio, usuarioEf);
                _context.EntityModified(usuarioEf);
                await _context.UpdateEntityAsync(usuarioEf);

                return OperationResult<string>.Ok("Edicion realizada con exito");
            });
        }
        public async Task<OperationResult<Usuario>> ObtenerUsuarioConProveedoresYPedidosAsync(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Pedidos)
                .Include(u => u.Proveedores)
                .FirstOrDefaultAsync(u => u.Id == id);

            return usuario is null
                ? OperationResult<Usuario>.Fail("Usuario no encontrado")
                : OperationResult<Usuario>.Ok("", usuario);
        }
    }
}
