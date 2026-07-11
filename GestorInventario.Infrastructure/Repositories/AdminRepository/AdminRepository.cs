using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace GestorInventario.Infrastructure.Repositories.AdminRepository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly GestorInventarioContext _context;       
        private readonly ILogger<AdminRepository> _logger;
    
        public AdminRepository(GestorInventarioContext context,   ILogger<AdminRepository> logger)
        {
            _context = context;            
            _logger = logger;     
         
        }
        public  IQueryable<Role> ObtenerRoles()
        {
            var roles = _context.Roles.Include(x => x.Usuarios).AsQueryable();
            return roles;
        }
        public IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId)
        {
            return _context.Usuarios
                .Where(u => u.IdRol == rolId)
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
        }
        public async Task<List<string>> ObtenerEmailsAdministradoresAsync(CancellationToken stoppingToken = default)
        {
           
            return await _context.Usuarios
                .Where(u => u.IdRolNavigation.Nombre == "Administrador" && u.Email != null)
                .Select(u => u.Email)
                .Distinct()
                .ToListAsync(stoppingToken);
        }
        public IQueryable<Usuario> ObtenerUsuarios()
        {
            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
        }
        public async Task<OperationResult<string>> EliminarUsuario(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var user = await _context.Usuarios.FindAsync(id);
                if (user == null)
                    return OperationResult<string>.Fail("El usuario no existe");

                await _context.DeleteEntityAsync(user);
                return OperationResult<string>.Ok("Usuario eliminado con exito");
            });
        }
        public async Task<OperationResult<string>> BajaUsuario(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuarioDB = await _context.Usuarios.FindAsync(id); ;
                if (usuarioDB is null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.BajaUsuario = true;

                await _context.UpdateEntityAsync(usuarioDB);

                return OperationResult<string>.Ok("Usuario dado de baja con exito");

            });         
        }
        public async Task<OperationResult<string>> AltaUsuario(int id)
        {

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuarioDB = await _context.Usuarios.FindAsync(id);
                if (usuarioDB is null )
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.BajaUsuario = false;

                await _context.UpdateEntityAsync(usuarioDB);

                return OperationResult<string>.Ok("Usuario dado de alta con exito");

            });
                
          }
          
        public async Task<OperationResult<Usuario>> ActualizarRolUsuario(int usuarioId, int rolId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario is null)
                {
                    return OperationResult<Usuario>.Fail("Usuario no encontrado");
                }
                var rol = await _context.Roles.FindAsync(rolId);
                if (rol == null)
                {
                    return OperationResult<Usuario>.Fail("Rol no encontrado");
                }

                usuario.IdRol = rolId;
                await _context.UpdateEntityAsync(usuario);              
                return OperationResult<Usuario>.Ok("", usuario);
            });
           
        }
        public async Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var proveedores = await _context.Proveedores
                .Where(p => p.IdUsuario == usuarioOrigenId)
                .ToListAsync();

                foreach (var proveedor in proveedores)
                {
                    proveedor.IdUsuario = usuarioDestinoId;
                    await _context.UpdateEntityAsync(proveedor);
                }
                return OperationResult<string>.Ok("Proveedores reasignados correctamente");
            });
           
        }
        public async Task<Usuario> ObtenerUsuarioConProveedoresYPedidosAsync(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Pedidos)
                .Include(u => u.Proveedores)
                .FirstOrDefaultAsync(u => u.Id == id);

            return usuario;
        }
    }     
}

