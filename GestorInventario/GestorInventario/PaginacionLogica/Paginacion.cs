namespace GestorInventario.PaginacionLogica
{
    public class Paginacion
    {
        public int Pagina { get; set; } = 1;
      
        public int CantidadAMostrar { get; set; } = 3;//Cantidad de registros por pagina
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int Radio { get; set; } = 3;
    }
}
