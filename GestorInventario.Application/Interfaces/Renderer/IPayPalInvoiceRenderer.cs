using GestorInventario.Application.DTOS;

namespace GestorInventario.Application.Interfaces.Renderer
{
    public interface IPayPalInvoiceRenderer
    {
        byte[] Render(FacturaPdfDto dto);
    }
}
