
using GestorInventario.Application.DTOs.Response_paypal;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.Paypal
{
    public class ProductosPaypalViewModel
    {
        public List<ProductoPaypalDto> Productos { get; set; } = new List<ProductoPaypalDto>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }

   
}
