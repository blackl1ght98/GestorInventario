using GestorInventario.PaginacionLogica;

namespace GestorInventario.Infraestructure.Utils
{
    public record PaginationResult<T>
    {
        public PaginationResult() { }

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
