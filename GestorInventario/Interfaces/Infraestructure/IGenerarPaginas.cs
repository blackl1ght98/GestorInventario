using GestorInventario.PaginacionLogica;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IGenerarPaginas
    {
        List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3);
        List<PaginasModel> GenerarListaPaginas(Paginacion paginacion);
    }
}
