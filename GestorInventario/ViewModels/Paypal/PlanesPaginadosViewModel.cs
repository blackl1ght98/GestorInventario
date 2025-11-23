using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.Paypal
{
    public class PlanesPaginadosViewModel
    {
        public List<PlanesDto> Planes { get; set; } = new List<PlanesDto>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
        public int CantidadAMostrar { get; set; }
    }
  
}
