using GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.Mapping
{
    public interface IPayPalOrderMappingService
    {
        PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago);
    }
}
