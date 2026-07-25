using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.MetodosPaginacion
{
    public interface IPageLinkGenerator
    {
        List<PaginasModel> GenerarListaPaginas(int totalPaginas, int paginaActual, int? radio = 3);
      
    }
}
