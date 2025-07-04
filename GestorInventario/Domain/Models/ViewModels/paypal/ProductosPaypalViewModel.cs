using GestorInventario.Domain.Models.ViewModels.product;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.Domain.Models.ViewModels.paypal
{
    public class ProductosPaypalViewModel
    {
        public List<ProductoPaypalViewModel> Productos { get; set; } = new List<ProductoPaypalViewModel>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }

    public class ProductoPaypalViewModel
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}
