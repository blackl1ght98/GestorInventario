using GestorInventario.Domain.Models; 
using GestorInventario.PaginacionLogica;
using System.Collections.Generic;

namespace GestorInventario.ViewModels
{
    public class RembolsosViewModel
    {
        public required List<Rembolso> Rembolsos { get; set; }  
        public required List<PaginasModel> Paginas { get; set; }  
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }
    }
}