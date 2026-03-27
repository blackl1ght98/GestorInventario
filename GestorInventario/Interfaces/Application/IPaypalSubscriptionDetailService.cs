using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalSubscriptionDetailService
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId);
    }
}
