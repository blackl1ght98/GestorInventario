using GestorInventario.Shared.DTOS.Paypal.Responses.POST.Subscription;
using System;
using System.Collections.Generic;
using System.Text;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Paypal.Plans
{
    public interface IPlanService
    {
        Task<OperationResult<string>> SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlanDto planPriceUpdate);
    }
}
