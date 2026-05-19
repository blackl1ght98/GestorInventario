using GestorInventario.Application.DTOs;

using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalSubscriptionService
    {
        Task<OperationResult<string>> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<OperationResult<string>> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, string intervalUnit, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<OperationResult<PaypalPlanDetailsDto>> ObtenerDetallesPlan(string id);
        Task<OperationResult<(PaypalProductListResponseDto ProductsResponse, bool HasNextPage)>> GetProductsAsync(int page = 1, int pageSize = 10);
        Task<OperationResult<(List<PaypalPlanResponseDto> plans, bool HasNextPage)>> GetSubscriptionPlansAsync(int page = 1, int pageSize = 6);
        Task<OperationResult<string>> EditarProducto(string id, string name, string description);
        Task<OperationResult<(string, UpdatePricingPlanDto)>> UpdatePricingPlanAsync(string planId, decimal? trialAmount, decimal regularAmount, string currency);
        Task<OperationResult<string>> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<OperationResult<PaypalSubscriptionResponse>> ObtenerDetallesSuscripcion(string subscription_id);
        Task<OperationResult<(string planId, string planStatus)>> DesactivarPlan(string planId);
        Task<OperationResult<(string planId, string planStatus)>> ActivarPlan(string planId);
        Task<OperationResult<(string subId, string subStatus)>> CancelarSuscripcion(string subscription_id, string reason);
        Task<OperationResult<(string subId, string subStatus)>> SuspenderSuscripcion(string subscription_id, string reason);
        Task<OperationResult<(string subId, string subStatus)>> ActivarSuscripcion(string subscription_id, string reason);
      
    }
}
