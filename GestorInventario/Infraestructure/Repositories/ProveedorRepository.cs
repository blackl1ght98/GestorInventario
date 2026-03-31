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
        private readonly IMapper _mapper;
        public ProveedorRepository(GestorInventarioContext context, ILogger<ProveedorRepository> logger, IMapper map)
        {
            _context = context;
            _logger = logger;
            _mapper = map;
        }

        public IQueryable<Proveedore> ObtenerProveedores()
        {
            return _context.Proveedores
                .Include(p => p.IdUsuarioNavigation); 
        }
       

        public async Task<OperationResult<string>> CrearProveedor(ProveedorViewModel model)
        {
           
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
               var existingProvider = await _context.Proveedores.FirstOrDefaultAsync(x=>x.NombreProveedor == model.NombreProveedor);
                if (existingProvider != null) {
                    return OperationResult<string>.Fail("Ya existe un proveedor registrado con ese nombre");
                
                }
                // Mapeo a Entidad de Dominio
                var proveedorDominio = _mapper.Map<EntityProveedor>(model);

                // Mapeo de Entidad de Dominio → Entidad EF para guardar
                var proveedor = _mapper.Map<Proveedore>(proveedorDominio);
                await _context.AddEntityAsync(proveedor);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Proveedor creado con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex,"Error al crear el proveedor");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Error al crear el proveedor");
            }
           
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
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
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
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Proveedor eliminado con exito");

            }
            catch (Exception ex)
            {

                _logger.LogError(ex,"Error al eliminar el proveedor");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Error al eliminar el proveedor");
            }
           
        }
        public async Task<OperationResult<string>> EditarProveedor(ProveedorViewModel model, int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Obtenemos la entidad EF rastreada
                var proveedorEf = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == Id);
                if (proveedorEf == null)
                {
                    return OperationResult<string>.Fail("El proveedor no existe");
                }

                // 2. Mapeamos a dominio para aplicar lógica (si la hubiera)
                var proveedorDominio = _mapper.Map<EntityProveedor>(proveedorEf);

                // 3. Actualizamos la entidad de dominio con los datos del ViewModel
                _mapper.Map(model, proveedorDominio);

                // 4. Copiamos los cambios de vuelta a la entidad EF rastreada
                _mapper.Map(proveedorDominio, proveedorEf);

                // 5. Guardamos
                await _context.UpdateEntityAsync(proveedorEf);
                await transaction.CommitAsync();

                return OperationResult<string>.Ok("Proveedor editado con éxito");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error de concurrencia al editar proveedor");

                var proveedorEf = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == Id);
                if (proveedorEf == null)
                {
                    return OperationResult<string>.Fail("El proveedor no existe");
                }

                _context.Entry(proveedorEf).Reload();
                _mapper.Map(model, proveedorEf);   

                await _context.UpdateEntityAsync(proveedorEf);
                await transaction.CommitAsync();

                return OperationResult<string>.Ok("Proveedor editado con éxito");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ocurrió un error inesperado al editar el proveedor");
                return OperationResult<string>.Fail("Ocurrió un error inesperado al editar el proveedor");
            }
        }

       
    }
}
