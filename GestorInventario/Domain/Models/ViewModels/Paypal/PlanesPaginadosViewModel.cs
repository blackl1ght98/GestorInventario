using GestorInventario.PaginacionLogica;

namespace GestorInventario.Domain.Models.ViewModels.Paypal
{
    public class PlanesPaginadosViewModel
    {
        public IEnumerable<PlanesViewModel> Planes { get; set; }
        public IEnumerable<PaginasModel> Paginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }
}
