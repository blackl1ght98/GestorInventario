namespace GestorInventario.Application.DTOS.Paypal
{
    public record RefundDto
    {
      
        public required string NumeroPedido { get; init; }
  
        public required string NombreCliente { get; init; }
      
        public required string EmailCliente { get; init; }
    
        public DateTime FechaRembolso { get; init; }
      
        public required string MotivoRembolso { get; init; }
    }
}
