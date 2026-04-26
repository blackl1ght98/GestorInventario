using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalService
    {
        Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails);
        Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo);
        Task UpdatePedidoStatusAsync(int pedidoId, string status, string refundId, string estadoVenta);
        Task RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string refundId, decimal montoReembolsado, string motivo, string estadoVenta);
        Task AddInfoTrackingOrder(int pedidoId, string tracking, string url, string carrier);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlanDto planPriceUpdate);
        Task SaveOrUpdateSubscriptionDetailsAsync(SubscriptionDetail subscriptionDetails);
        Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId);
        Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);
    }
}
