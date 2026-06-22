using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalService
    {
        Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails);
     
     
       
        Task UpdatePlanStatusAsync(string planId, string status);
        Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlanDto planPriceUpdate);
        Task SaveOrUpdateSubscriptionDetailsAsync(SubscriptionDetail subscriptionDetails);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);
       
    }
}
