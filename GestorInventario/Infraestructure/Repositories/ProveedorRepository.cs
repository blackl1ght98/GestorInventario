using AutoMapper;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.provider;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly ILogger<ProveedorRepository> _logger;
     
        public ProveedorRepository(GestorInventarioContext context, ILogger<ProveedorRepository> logger)
        {
            _context = context;
            _logger = logger;
           
        }

        public IQueryable<Proveedore> ObtenerProveedores()
        {
            return _context.Proveedores
                .Include(p => p.IdUsuarioNavigation); 
        }


        public async Task<OperationResult<string>> CrearProveedor(ProveedorViewModel model)
        {

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var existingProvider = await _context.Proveedores.FirstOrDefaultAsync(x => x.NombreProveedor == model.NombreProveedor);
                if (existingProvider != null)
                {
                    return OperationResult<string>.Fail("Ya existe un proveedor registrado con ese nombre");

                }
                var proveedor = new Proveedore()
                {
                    NombreProveedor = model.NombreProveedor,
                    Contacto = model.Contacto,
                    Direccion = model.Direccion,
                    IdUsuario = model.IdUsuario
                };
                await _context.AddEntityAsync(proveedor);
              
                return OperationResult<string>.Ok("Proveedor creado con exito");
            });
           

        }
        public async Task<OperationResult<Proveedore>> ObtenerProveedorId(int id)
        {
            var proveedor= await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);
            if (proveedor is null) 
            {
                return OperationResult<Proveedore>.Fail("Proveedor no encontrado");
            
            }
            return OperationResult<Proveedore>.Ok("",proveedor);
        }

        public async Task<OperationResult<string>> EliminarProveedor(int Id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var proveedor = await _context.Proveedores.Include(p => p.Productos).FirstOrDefaultAsync(m => m.Id == Id);
                if (proveedor == null)
                {
                    return OperationResult<string>.Fail("Proveedor no encontrado");
                }
                if (proveedor.Productos.Any())
                {
                    return OperationResult<string>.Fail("El proveedor no se puede eliminar porque tiene productos asociados");
                }
                await _context.DeleteEntityAsync(proveedor);
              
                return OperationResult<string>.Ok("Proveedor eliminado con exito");

            });
            
           
        }
        public async Task<OperationResult<string>> EditarProveedor(ProveedorViewModel model, int Id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var proveedor = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == Id);
                if (proveedor == null)

                {
                    return OperationResult<string>.Fail("El proveedor no existe");
                }

                await ActualizarProveedor(proveedor, model);
        

                return OperationResult<string>.Ok("Proveedor editado con éxito");
            });
          
        }
        private async Task ActualizarProveedor(Proveedore proveedor, ProveedorViewModel model)
        {
            proveedor.NombreProveedor = model.NombreProveedor;
            proveedor.Contacto = model.Contacto;
            proveedor.Direccion = model.Direccion;
            proveedor.IdUsuario = model.IdUsuario;
            await _context.UpdateEntityAsync(proveedor);

        }
    }

}

