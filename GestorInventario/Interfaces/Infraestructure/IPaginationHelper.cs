using GestorInventario.PaginacionLogica;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaginationHelper
    {
        Task<PaginationResult<T>> PaginarAsync<T>(IQueryable<T> query, Paginacion paginacion);
    
        PaginationResult<T> PaginarSinTotal<T>(List<T> items,int paginaActual,bool hasNextPage,int cantidadAMostrar,int radio = 3);
    }
}
