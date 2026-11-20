using GestorInventario.Application.DTOS.Supplier;


using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Supplier
{
    public class SupplierIndexViewModel
    {
        public required List<ProveedorCardViewmodelDto> Proveedores { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }
   
    }
}
