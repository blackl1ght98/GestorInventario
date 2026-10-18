using GestorInventario.Application.DTOS;
using GestorInventario.Domain.Models;

namespace GestorInventario.Infrastructure.Caching;

public static class PaypalMappingExtensions
{
   
    
    public static TrackingItemDto ToDto(this DetallePedido detalle)
    {
        return new TrackingItemDto
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