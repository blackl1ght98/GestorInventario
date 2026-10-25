using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Domain.enums.Usuario;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
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
                .Where(u => u.IdRolNavigation.Nombre == Rol.Administrador.ToString() && u.Email != null)
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
        public async Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // Una sola UPDATE en lugar de N round-trips por cada proveedor.
                var filasAfectadas = await _context.Proveedores
                    .Where(p => p.IdUsuario == usuarioOrigenId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.IdUsuario, usuarioDestinoId));

                return OperationResult<string>.Ok(
                    $"Proveedores reasignados correctamente ({filasAfectadas} actualizados)");
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
    
        public async Task<Usuario> ObtenerUsuarioPorId(int id)
        {
            var usuario = await _context.Usuarios.AsTracking()
                .Include(x => x.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);
            return usuario;
        }  
        public async Task<Usuario> ObtenerEmail(string email)
        {
            var correo = await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == email); 
            return  correo;

        }
        public async Task<bool> AnyAsync() => await _context.Usuarios.AnyAsync();
          public async Task<List<Usuario>> ObtenerUsuariosAsync() 
        { 
            var listaUsuarios= await _context.Usuarios.Include(u => u.IdRolNavigation).ToListAsync();
            return listaUsuarios;

        }
        public async Task<List<string>> ObtenerEmailsAdministradoresAsync()
        {
            return await _context.Usuarios
                .Where(u => u.IdRolNavigation.Nombre == Rol.Administrador.ToString())
                .Select(u => u.Email)
                .ToListAsync();
        }
      
        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(x => x.Email == email);
        }
        public async Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(usuario);
                return OperationResult<Usuario>.Ok( usuario,"Usuario guardado");
            });
        }
        public async Task<OperationResult<Usuario>> EliminarUsuarioAsync(Usuario usuario)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(usuario);
                return OperationResult<Usuario>.Ok( usuario);
            });
        }
        public async Task<OperationResult<string>> ActualizarUsuarioAsync(Usuario usuario)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.EntityModified(usuario);
                await _context.UpdateEntityAsync(usuario);
                return OperationResult<string>.Ok("Edicion realizada con exito");
            });
        }

    }     
}

