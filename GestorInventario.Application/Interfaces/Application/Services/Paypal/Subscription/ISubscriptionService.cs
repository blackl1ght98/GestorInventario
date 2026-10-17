using GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.Subscription;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.Subscription
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);

    }
}
