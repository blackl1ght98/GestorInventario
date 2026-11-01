using GestorInventario.Application.DTOS;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Renderer.PDF
{
    public interface IPayPalInvoiceRenderer
    {
        byte[] Render(FacturaPdfDto dto);
    }
}
