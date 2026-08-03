using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;

namespace GestorInventario.Interfaces.Application.Services.Paypal.Mapping
{
    public interface IPayPalOrderMappingService
    {
        PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago);
    }
}
