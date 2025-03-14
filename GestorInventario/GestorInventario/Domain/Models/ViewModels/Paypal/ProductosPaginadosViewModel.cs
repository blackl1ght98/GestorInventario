using GestorInventario.PaginacionLogica;

namespace GestorInventario.Domain.Models.ViewModels.Paypal
{
    public class ProductosPaginadosViewModel
    {
        public IEnumerable<ProductoViewModel> Productos { get; set; }
        public IEnumerable<PaginasModel> Paginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }
}
