
using GestorInventario.Shared.DTOS.Paypal.Projections;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Paypal
{
    public class PayPalProductsIndexViewModel
    {
        public List<ProductoProjection> Productos { get; set; } = new List<ProductoProjection>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }

   
}
