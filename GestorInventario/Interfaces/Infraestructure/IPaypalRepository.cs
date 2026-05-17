using GestorInventario.Application.DTOs;

using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalRepository
    {
        //Consultas
        Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId);
        IQueryable<SubscriptionDetail> ObtenerSubscripciones();
        IQueryable<UserSubscription> ObtenerSubscripcionesUsuario(int usuarioId);
        Task<PlanDetail> ObtenerPlanPorIdAsync(string planId);
        Task<SubscriptionDetail> ObtenerSubscriptionIdAsync(string subscriptionId);
        Task<Rembolso> ObtenRembolsoAsync(string numeroPedido);
        Task<UserSubscription> ObtenerSubscricionUsuarioAsync(int userId, string subscriptionId);
        Task<List<UserSubscription>> ObtenerSusbcripcionesUsuario(string planId);
        //Operaciones
        Task<OperationResult<PlanDetail>> AgregarPlanAsync(PlanDetail plan);
        Task<OperationResult<Rembolso>> AgregarRembolsoAsync(Rembolso rembolso);
        Task<OperationResult<Rembolso>> ActualizarRembolsoAsync(Rembolso rembolso);     
        Task<OperationResult<SubscriptionDetail>> AgregarDetallesSubscripcionAsync(SubscriptionDetail subscripcion);
        Task<OperationResult<SubscriptionDetail>> ActualizarDetallesSubscripcionAsync(SubscriptionDetail subscripcion);
        Task<OperationResult<PlanDetail>> ActualizarPlanAsync(PlanDetail plan);
        Task<OperationResult<UserSubscription>> AgregarSubscripcionUsuarioAsync(UserSubscription subscripcion);
        List<string> GetCategoriesFromEnum();

    }
}
