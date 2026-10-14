using GestorInventario.Domain.Models;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Order;

namespace GestorInventario.Interfaces.Application.Services.Paypal.Mapping
{
    public interface IPayPalOrderMappingService
    {
        PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago);
    }
}
