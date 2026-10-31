using GestorInventario.Application.DTOS;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Paypal
{
    public class UserSubscriptionsIndexViewModel
    {
        
        public List<SubcritionDtoViewModel> Suscripciones { get; set; } = new List<SubcritionDtoViewModel>();
        public List<PaginasModel> Paginas { get; set; } = new List<PaginasModel>();
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
        public int CantidadAMostrar { get; set; }
    }
}
