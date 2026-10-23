using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Paypal
{
    public class PayPalProductsIndexViewModel
    {
        public List<ProductoPaypalDto> Productos { get; set; } = new List<ProductoPaypalDto>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }

   
}
