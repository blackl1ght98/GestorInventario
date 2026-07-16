using GestorInventario.Domain.Models;


namespace GestorInventario.Interfaces.Renderer
{
    public interface IPayPalInvoiceRenderer
    {
        byte[] Render(PayPalPaymentDetail data);
    }
}
