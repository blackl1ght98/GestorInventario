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
        public PaginationResult<T> PaginarLista<T>(
    IEnumerable<T> lista,
    Paginacion paginacion)
        {
            var totalItems = lista.Count();
            var totalPaginas = (int)Math.Ceiling((double)totalItems / paginacion.CantidadAMostrar);

            if (paginacion.Pagina < 1)
                paginacion.Pagina = 1;
            else if (paginacion.Pagina > totalPaginas && totalPaginas > 0)
                paginacion.Pagina = totalPaginas;

            var items = lista
                .Skip((paginacion.Pagina - 1) * paginacion.CantidadAMostrar)
                .Take(paginacion.CantidadAMostrar)
                .ToList();

            var paginas = _generarPaginas.GenerarListaPaginas(totalPaginas, paginacion.Pagina, paginacion.Radio);

            return new PaginationResult<T>(items, totalItems, totalPaginas, paginacion.Pagina, paginas);
        }


    }

    public record PaginationResult<T>
    {
        public PaginationResult() { } // 👈 NECESARIO PARA POLLY

        public PaginationResult(List<T> items, int totalItems, int totalPaginas, int paginaActual, IEnumerable<PaginasModel> paginas)
        {
            Items = items;
            TotalItems = totalItems;
            TotalPaginas = totalPaginas;
            PaginaActual = paginaActual;
            Paginas = paginas;
        }

        public List<T> Items { get; init; }
        public int TotalItems { get; init; }
        public int TotalPaginas { get; init; }
        public int PaginaActual { get; init; }
        public IEnumerable<PaginasModel> Paginas { get; init; }
    }

}

