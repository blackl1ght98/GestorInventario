using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOS
{
    public class RefundDto
    {
      
        public required string NumeroPedido { get; set; }
  
        public required string NombreCliente { get; set; }
      
        public required string EmailCliente { get; set; }
    
        public DateTime FechaRembolso { get; set; }
      
        public required string MotivoRembolso { get; set; }
    }
}
