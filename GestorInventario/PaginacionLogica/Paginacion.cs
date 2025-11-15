namespace GestorInventario.PaginacionLogica
{
    public class Paginacion
    {
        private int _pagina = 1;
        private int _cantidadAMostrar = 6;

        public int Pagina
        {
            get => _pagina;
            set => _pagina = value < 1 ? 1 : value; // 🔹 Evita páginas negativas o 0
        }

        public int CantidadAMostrar
        {
            get => _cantidadAMostrar;
            set => _cantidadAMostrar = value <= 0 ? 6 : value; // 🔹 Evita cantidad <= 0
        }

        public int Radio { get; set; } = 3;

        public int TotalPaginas { get; set; }
    }
}
