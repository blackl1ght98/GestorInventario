using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalRepository
    {
        Task<OperationResult<PlanDetail>> AgregarPlanAsync(PlanDetail plan);
        Task<OperationResult<Rembolso>> AgregarRembolsoAsync(Rembolso rembolso);
        Task<OperationResult<Rembolso>> ActualizarRembolsoAsync(Rembolso rembolso);
        Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId);
        Task<List<UserSubscription>> SusbcripcionesUsuario(string planId);
        IQueryable<SubscriptionDetail> ObtenerSubscripciones();
        IQueryable<UserSubscription> ObtenerSubscripcionesUsuario(int usuarioId);
        Task<PlanDetail> ObtenerPlanPorIdAsync(string planId);
        Task<OperationResult<SubscriptionDetail>> AgregarDetallesSubscripcionAsync(SubscriptionDetail subscripcion);
        Task<OperationResult<SubscriptionDetail>> ActualizarDetallesSubscripcionAsync(SubscriptionDetail subscripcion);
        Task<OperationResult<PlanDetail>> ActualizarPlanAsync(PlanDetail plan);
        Task<SubscriptionDetail> ObtenerSubscriptionIdAsync(string subscriptionId);
        Task<Rembolso> ObtenRembolsoAsync(string numeroPedido);
        Task<UserSubscription> ObtenerSubscricionUsuarioAsync(int userId, string subscriptionId);
        Task<OperationResult<UserSubscription>> AgregarSubscripcionUsuarioAsync(UserSubscription subscripcion);
        List<string> GetCategoriesFromEnum();
     
 
           
      
      
    
    }
}
