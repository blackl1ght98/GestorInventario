
using GestorInventario.Domain.Models;
using GestorInventario.Infrestructura;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;

using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories.ProveedorRepository
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly GestorInventarioContext _context;
       
     
        public ProveedorRepository(GestorInventarioContext context)
        {
            _context = context;
           
           
        }

        public IQueryable<Proveedore> ObtenerProveedores()
        {
            return _context.Proveedores
                .Include(p => p.IdUsuarioNavigation); 
        }
        public async Task<Proveedore> ObtenerProveedorId(int id)
        {
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);         
            return  proveedor;
        }

        public async Task<OperationResult<string>> CrearProveedor(CrearProveedorDto model)
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
        public async Task<OperationResult<string>> EditarProveedor(EditarProveedorDto model, int Id)
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
        private async Task ActualizarProveedor(Proveedore proveedor, EditarProveedorDto model)
        {
            proveedor.NombreProveedor = model.NombreProveedor;
            proveedor.Contacto = model.Contacto;
            proveedor.Direccion = model.Direccion;
            proveedor.IdUsuario = model.IdUsuario;
            await _context.UpdateEntityAsync(proveedor);

        }
    }

}

