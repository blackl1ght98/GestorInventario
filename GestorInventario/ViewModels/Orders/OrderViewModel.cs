using GestorInventario.Application.DTOS;
using GestorInventario.Domain.Models;

using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Orders
{
    public class OrderViewModel
    {
        public required List<PedidoDtoViewModel> Pedidos { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public required List<string> Carrier { get; set; } 
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }    
    }
}
