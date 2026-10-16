using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Renderer.PDF
{
    public interface IPayPalInvoiceRenderer
    {
        byte[] Render(PayPalPaymentDetail data);
    }
}
