using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPayPalOrderMappingService
    {
        PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago);
    }
}
