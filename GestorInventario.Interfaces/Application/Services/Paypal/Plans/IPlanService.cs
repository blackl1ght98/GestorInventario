using System;
using System.Collections.Generic;
using System.Text;
using GestorInventario.Shared.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Subscription;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Paypal.Plans
{
    public interface IPlanService
    {
        Task<OperationResult<string>> SavePlanDetailsAsync(string planId, PaypalPlanDetailsResponse planDetails);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlanRequest planPriceUpdate);
    }
}
