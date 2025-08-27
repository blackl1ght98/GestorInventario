using GestorInventario.Domain.Models; 
using GestorInventario.PaginacionLogica;
using System.Collections.Generic;

namespace GestorInventario.ViewModels
{
    public class RembolsosViewModel
    {
        public List<Rembolso> Rembolsos { get; set; }  
        public List<PaginasModel> Paginas { get; set; }  
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public string Buscar { get; set; }
    }
}