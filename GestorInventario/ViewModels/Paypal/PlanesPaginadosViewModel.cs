using GestorInventario.Application.DTOS.Paypal.Projections;
using GestorInventario.PaginacionLogica;



namespace GestorInventario.ViewModels.Paypal
{
    public class PlanesPaginadosViewModel
    {
        public List<PlanProjection> Planes { get; set; } = new List<PlanProjection>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int CantidadAMostrar { get; set; }
    }
  
}
