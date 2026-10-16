using GestorInventario.Domain.Models;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Order;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.Mapping
{
    public interface IPayPalOrderMappingService
    {
        PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago);
    }
}
