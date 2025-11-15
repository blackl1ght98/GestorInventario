using Microsoft.EntityFrameworkCore;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.MetodosExtension
{
    public static class QueryableExtensions
    {
        public static async Task<(List<T> Items, int TotalItems)> PaginarAsync<T>(
     this IQueryable<T> queryable, Paginacion paginacion, bool calcularTotal = true)
        {
            int totalItems = calcularTotal ? await queryable.CountAsync() : 0;

            var items = await queryable
                .Skip((paginacion.Pagina - 1) * paginacion.CantidadAMostrar)
                .Take(paginacion.CantidadAMostrar)
                .ToListAsync();

            return (items, totalItems);
        }

        // Mantener el método Paginar actual para casos donde no se necesite el conteo
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, Paginacion paginacion)
        {
            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;
            return queryable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }

        // Mantener PaginarLista para colecciones en memoria (si es necesario)
        public static IEnumerable<T> PaginarLista<T>(this IEnumerable<T> enumerable, Paginacion paginacion)
        {
            int skip = (paginacion.Pagina - 1) * paginacion.CantidadAMostrar;
            return enumerable.Skip(skip).Take(paginacion.CantidadAMostrar);
        }
    }
}