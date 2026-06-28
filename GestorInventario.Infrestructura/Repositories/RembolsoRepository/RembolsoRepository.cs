using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;

using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrestructura.Repositories.RembolsoRepository
{
    public class RembolsoRepository:IRembolsoRepository
    {
        private readonly GestorInventarioContext _context;
       

        public RembolsoRepository(GestorInventarioContext context)
        {
            _context = context;
           
        }

        public Task<IQueryable<Rembolso>> ObtenerRembolsos()
        {
             return Task.FromResult(_context.Rembolsos
            .Include(x => x.Pedido)  
            .AsQueryable());
        }
        public async Task<OperationResult<string>> EliminarRembolso(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var rembolso = await _context.Rembolsos.FindAsync(id);
                if (rembolso == null)
                {
                    return OperationResult<string>.Fail("El rembolso no existe");
                }
                await _context.DeleteEntityAsync(rembolso);             
                return OperationResult<string>.Ok("Rembolso eliminado con exito");
            });
          

        }
    }
}
