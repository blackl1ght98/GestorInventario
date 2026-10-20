using GestorInventario.Application.DTOS;

using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.Plans
{
    public interface IPlanService
    {
        Task<OperationResult<string>> SavePlanDetailsAsync(string planId, PlanDetailsDto planDetails);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task SavePlanPriceUpdateAsync(PlanPriceUpdateResultDto planPriceUpdate);
    }
}
