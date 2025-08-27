using GestorInventario.Domain.Models;
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
            return Task.FromResult(_context.Rembolsos.AsQueryable());
        }
        public async Task<(bool, string)> EliminarRembolso(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var rembolso = await _context.Rembolsos.FindAsync(id);
                if (rembolso == null)
                {
                    return (false, "El rembolso que intenta eliminar no existe");
                }
              

                await _context.DeleteEntityAsync(rembolso);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al dar de baja el usuario");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }

        }
    }
}
