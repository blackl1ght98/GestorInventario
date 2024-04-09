using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CrudOperation: IAdminCrudOperation
    {
        private readonly GestorInventarioContext _context;

        public CrudOperation(GestorInventarioContext context)
        {
            _context = context;
        }
        public void UpdateOperation(Usuario usuario)
        {
             _context.UpdateEntity(usuario);

        }
        public void AddOperation(Usuario usuario)
        {
            _context.AddEntity(usuario);
        }
        public void  ReloadEntity(Usuario usuario)
        {
            _context.Entry(usuario).Reload();
        }
        public void ModifyEntityState(Usuario usuario, EntityState entityState)
        {
            _context.Entry(usuario).State = entityState;
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public void  DeleteOperation(Usuario usuario)
        {
            _context.DeleteEntity(usuario);
        }

    }
}
