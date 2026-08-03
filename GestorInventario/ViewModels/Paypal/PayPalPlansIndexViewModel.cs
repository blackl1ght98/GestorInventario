
using GestorInventario.Shared.DTOS.Paypal.Projections;
using GestorInventario.Shared.Utilities;



namespace GestorInventario.ViewModels.Paypal
{
    public class PayPalPlansIndexViewModel
    {
        public List<PlanProjection> Planes { get; set; } = new List<PlanProjection>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int CantidadAMostrar { get; set; }
    }
  
}
