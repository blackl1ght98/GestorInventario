using GestorInventario.Application.Services;
using GestorInventario.PaginacionLogica;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Utils
{
    public class PaginationHelper
    {
        private readonly GenerarPaginas _generarPaginas;

        public PaginationHelper(GenerarPaginas generarPaginas)
        {
            _generarPaginas = generarPaginas;
        }

        public async Task<PaginationResult<T>> PaginarAsync<T>(
     IQueryable<T> query,
     Paginacion paginacion)
        {
            var totalItems = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);

            // ✅ Validación interna
            if (paginacion.Pagina < 1)
                paginacion.Pagina = 1;
            else if (paginacion.Pagina > totalPaginas && totalPaginas > 0)
                paginacion.Pagina = totalPaginas;

            var items = await query
                .Skip((paginacion.Pagina - 1) * paginacion.CantidadAMostrar)
                .Take(paginacion.CantidadAMostrar)
                .ToListAsync();

            var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

            return new PaginationResult<T>(items, totalItems, totalPaginas, paginacion.Pagina, paginas);
        }

    }

    public record PaginationResult<T>(
     List<T> Items,
     int TotalItems,
     int TotalPaginas,
     int PaginaActual,
     IEnumerable<PaginasModel> Paginas);
}

