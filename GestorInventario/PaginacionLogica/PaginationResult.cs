namespace GestorInventario.PaginacionLogica
{
    public record PaginationResult<T>
    {
       
        public List<T> Items { get; init; }
        public int? TotalItems { get; init; }
        public int TotalPaginas { get; init; }
        public int PaginaActual { get; init; }
        public IEnumerable<PaginasModel> Paginas { get; init; }
        public PaginationResult() { }
        // Constructor completo (con totalItems conocido)
        public PaginationResult(List<T> items, int? totalItems, int totalPaginas, int paginaActual, IEnumerable<PaginasModel> paginas)
        {
            Items = items;
            TotalItems = totalItems;
            TotalPaginas = totalPaginas;
            PaginaActual = paginaActual;
            Paginas = paginas;
        }

        // Constructor para APIs externas (sin total)
        public PaginationResult(List<T> items, int totalPaginas, int paginaActual, IEnumerable<PaginasModel> paginas)
            : this(items, null, totalPaginas, paginaActual, paginas)  
        {
        }

    }
}
