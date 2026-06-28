using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPayPalOrderMappingService
    {
        PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago);
    }
}
