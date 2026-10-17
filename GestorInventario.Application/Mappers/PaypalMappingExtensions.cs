using GestorInventario.Application.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Mappers;

public static class PaypalMappingExtensions
{
    public static PayPalTrackingItemRequest ToPayPalRequest(this DetallePedido detalle)
    {
        return new PayPalTrackingItemRequest
        {
            Name = detalle.Producto?.NombreProducto ?? "Producto no disponible",
            Sku = detalle.Producto?.Descripcion ?? "N/A",
            Quantity = detalle.Cantidad,
            BarcodeType = detalle.Producto?.TipoCodigoBarras ?? "CODE_128",
            BarcodeCode = detalle.Producto?.CodigoBarras ?? string.Empty,
            ImageUrl = detalle.Producto?.Imagen ?? string.Empty,
            Url = string.Empty
        };
    }
  
}