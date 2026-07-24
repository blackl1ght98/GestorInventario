using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Interfaces.Application.Services.Paypal
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);

    }
}
