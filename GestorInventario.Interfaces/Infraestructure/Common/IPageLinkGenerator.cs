using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Infraestructure.Common
{
    public interface IPageLinkGenerator
    {
        List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3);
      
    }
}
