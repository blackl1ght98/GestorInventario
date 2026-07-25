using GestorInventario.Domain.Models;


namespace GestorInventario.Interfaces.Renderer.PDF
{
    public interface IPayPalInvoiceRenderer
    {
        byte[] Render(PayPalPaymentDetail data);
    }
}
