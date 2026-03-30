
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.PaginacionLogica
{
    public class PaginationHelper: IPaginationHelper
    {
        private readonly IPageLinkGenerator _pageLinkGenerator;

        public PaginationHelper(IPageLinkGenerator generarPaginas)
        {
            _pageLinkGenerator = generarPaginas;
        }

        public async Task<PaginationResult<T>> PaginarAsync<T>(IQueryable<T> query, Paginacion paginacion)
        {
            
            if (!query.Expression.ToString().Contains("OrderBy"))
            {
                query = query.OrderBy(e => EF.Property<object>(e, "Id"));   
            }

            var totalItems = await query.CountAsync();

            var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);

            // Validación de página
            if (paginacion.Pagina < 1)
                paginacion.Pagina = 1;
            else if (paginacion.Pagina > totalPaginas && totalPaginas > 0)
                paginacion.Pagina = totalPaginas;

            var items = await query
                .Skip((paginacion.Pagina - 1) * paginacion.CantidadAMostrar)
                .Take(paginacion.CantidadAMostrar)
                .ToListAsync();

            var paginas = _pageLinkGenerator.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

            return new PaginationResult<T>(items, totalItems, totalPaginas, paginacion.Pagina, paginas);
        }

        public PaginationResult<T> PaginarSinTotal<T>(List<T> items,int paginaActual,bool hasNextPage,int cantidadAMostrar,int radio = 3)
        {
            // Estimamos el total de páginas
            // Si hay página siguiente → asumimos al menos una más
            // Si no hay → estamos en la última
            int totalPaginasEstimado = hasNextPage ? paginaActual + 1 : paginaActual;

            // Validación de página (por seguridad)
            if (paginaActual < 1)
                paginaActual = 1;

            // Generamos las páginas visibles usando la estimación
            var paginas = _pageLinkGenerator.GenerarListaPaginas(
                totalPaginas: totalPaginasEstimado,
                paginaActual: paginaActual,
                radio: radio
            );

            return new PaginationResult<T>(
                items: items,
                totalItems: null,                    
                totalPaginas: totalPaginasEstimado,
                paginaActual: paginaActual,
                paginas: paginas
            );
        }


    }

   

}

