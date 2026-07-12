using GestorInventario.Domain.Models;

using GestorInventario.Shared.Utilities;


namespace GestorInventario.ViewModels.Refunds
{
    public class RefundsViewModel
    {
        public required List<Rembolso> Rembolsos { get; set; }  
        public required List<PaginasModel> Paginas { get; set; }  
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }
    }
}