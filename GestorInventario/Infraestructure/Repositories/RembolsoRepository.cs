using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class RembolsoRepository:IRembolsoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly ILogger<RembolsoRepository> _logger;

        public RembolsoRepository(GestorInventarioContext context, ILogger<RembolsoRepository> logger)
        {
            _context = context;
            _logger = logger;
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
