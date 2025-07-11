using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.Paypal
{
    public class SuscripcionesPaginadosViewModel
    {
        public List<SubscriptionDetail> Suscripciones { get; set; } = new List<SubscriptionDetail>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
        public int CantidadAMostrar { get; set; }
    }
}
