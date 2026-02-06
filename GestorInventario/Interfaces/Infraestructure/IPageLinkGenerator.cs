using GestorInventario.PaginacionLogica;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPageLinkGenerator
    {
        List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3);
      
    }
}
