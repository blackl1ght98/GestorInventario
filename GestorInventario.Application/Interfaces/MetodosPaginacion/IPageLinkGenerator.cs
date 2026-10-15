using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.MetodosPaginacion
{
    public interface IPageLinkGenerator
    {
        List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3);
      
    }
}
