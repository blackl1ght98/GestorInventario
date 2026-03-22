using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(dynamic subscriptionDetails, string planId);
    }
}
