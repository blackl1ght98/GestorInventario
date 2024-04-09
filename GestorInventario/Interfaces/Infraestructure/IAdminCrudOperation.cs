using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminCrudOperation
    {
        void UpdateOperation (Usuario usuario); 
        void AddOperation(Usuario usuario);
        void DeleteOperation(Usuario usuario);
        void ReloadEntity(Usuario usuario);
        void ModifyEntityState(Usuario usuario, EntityState entityState);
        Task SaveChangesAsync();
    }
}
