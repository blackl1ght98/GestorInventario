using GestorInventario.Infraestructure.Utils;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaginationHelper
    {
        Task<PaginationResult<T>> PaginarAsync<T>(IQueryable<T> query, Paginacion paginacion);
        PaginationResult<T> PaginarLista<T>(IEnumerable<T> lista, Paginacion paginacion);
    }
}
