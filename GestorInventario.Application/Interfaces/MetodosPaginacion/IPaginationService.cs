using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.MetodosPaginacion
{
    public interface IPaginationService
    {
        PaginationResult<T> PaginarSinTotal<T>(List<T> items,int paginaActual,bool hasNextPage,int cantidadAMostrar,int radio = 3);
        Task<PaginationResult<T>> PaginarAsync<T>(IQueryable<T> query, Paginacion paginacion);
    }
}
