using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {
        Task<string?> ObtenerEmailUsuarioAsync(int usuarioId);
        Task<Pedido> ObtenerNumeroPedido(RefundForm form);
        decimal? ConvertToDecimal(object value);
        int? ConvertToInt(object value);
        DateTime? ConvertToDateTime(object value);

    }
}
