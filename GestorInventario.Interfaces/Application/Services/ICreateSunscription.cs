using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface ICreateSunscription
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId);
    }
}
