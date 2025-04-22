using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalController
    {
        Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId);
        Task<List<UserSubscription>> SusbcripcionesUsuario(string planId);
        Task<SubscriptionDetail> ObtenerSubscripcion(string subscription_id);
        Task DetallesSubscripcion(string id);
        Task<(bool, string, SubscriptionDetail)> ObtenerDetallesSubscripcion(string id);
    }
}
