using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface IPlanService
    {
        Task<OperationResult<string>> SavePlanDetailsAsync(string planId, PlanDetailsDto planDetails);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task<OperationResult<string>> ActualizarPrecioPlanAsync(UpdatePlanPriceRequestDto request);
    }
}
