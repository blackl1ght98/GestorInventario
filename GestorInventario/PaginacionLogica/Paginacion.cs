namespace GestorInventario.PaginacionLogica
{
    public class Paginacion
    {
        public int Pagina { get; set; } = 1;
        public int CantidadAMostrar { get; set; } = 6; // Cantidad de registros por página
        public int Radio { get; set; } = 3;
        public int TotalPaginas { get; set; }
    }
}
