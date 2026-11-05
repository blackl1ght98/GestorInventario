using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionDto subscriptionDetails, string planId);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);

    }
}
