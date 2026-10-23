using GestorInventario.Application.DTOS.Paypal;

namespace GestorInventario.Application.Projections;

public static class PaypalProductExtensions
{ 
    public static ProductoPaypalDto ToProjection(this ProductSummaryDto product)
    {
        if (product == null) return null;

        return new ProductoPaypalDto
        {
            Id = product.Id,
            Nombre = product.Name,
            Descripcion = product.Description
        };
    }
}