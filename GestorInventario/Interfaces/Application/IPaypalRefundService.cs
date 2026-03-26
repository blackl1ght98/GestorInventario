namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalRefundService
    {
        Task<string> RefundSaleAsync(int pedidoId, string currency);
        Task<string> RefundPartialAsync(int pedidoId, string currency, string motivo);
    }
}
