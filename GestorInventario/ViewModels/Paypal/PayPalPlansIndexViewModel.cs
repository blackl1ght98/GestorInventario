
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Shared.Utilities;



namespace GestorInventario.ViewModels.Paypal
{
    public class PayPalPlansIndexViewModel
    {
        public List<PlanPaypalDto> Planes { get; set; } = new List<PlanPaypalDto>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int CantidadAMostrar { get; set; }
    }
  
}
