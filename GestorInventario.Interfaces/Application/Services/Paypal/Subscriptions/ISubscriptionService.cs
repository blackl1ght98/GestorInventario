using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;


namespace GestorInventario.Interfaces.Application.Services.Paypal.Subscriptions
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);

    }
}
