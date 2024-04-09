namespace GestorInventario.PaginacionLogica
{
    public class Paginacion
    {
        public int Pagina { get; set; } = 1;
        //Cuantos datos se va a mostrar por pagina
        public int CantidadAMostrar { get; set; } = 3;
    }
}
