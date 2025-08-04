using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalRepository
    {
        Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId);
        Task<List<UserSubscription>> SusbcripcionesUsuario(string planId);
        Task<SubscriptionDetail> ObtenerSubscripcion(string subscription_id);
        Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlan planPriceUpdate);
        Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails);
        Task UpdatePlanStatusAsync(string planId, string status);
        Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId);
        Task UpdatePedidoStatusAsync(int pedidoId, string status, string refundId);
        Task UpdatePlanStatusInDatabase(string planId, string status);
        Task<PlanDetail> ObtenerPlan(string planId);
    }
}
