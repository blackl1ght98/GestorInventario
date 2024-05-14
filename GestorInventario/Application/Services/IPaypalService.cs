using PayPal.Api;

namespace GestorInventario.Application.Services
{
    public interface IPaypalService
    {
        Task<Payment> CreateDonation(decimal amount, string returnUrl, string cancelUrl, string currency);
        Task<Payment> CreateOrderAsync(List<Item> items, decimal amount, string returnUrl, string cancelUrl, string currency);
    }
}
