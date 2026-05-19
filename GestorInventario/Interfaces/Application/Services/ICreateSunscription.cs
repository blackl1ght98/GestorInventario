using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface ICreateSunscription
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId);
    }
}
