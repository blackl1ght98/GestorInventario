using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Application.Mappers;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;


namespace GestorInventario.Application.Services.Common
{
    public class CreateSunscription:ICreateSunscription
    {
        
       
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<CreateSunscription> _logger;
        private readonly IPaypalService _paypalService;
        public CreateSunscription(IPaypalRepository paypalRepository, ILogger<CreateSunscription> logger, IPaypalService paypalService)
        {
           
          
            _paypalRepository = paypalRepository;
            _logger = logger;
            _paypalService = paypalService;
        }

        public async Task<SubscriptionDetail> CreateSubscriptionDetailAsync(
         PaypalSubscriptionResponse subscriptionDetails,
         string planId)
        {
            try
            {
                var plan = await _paypalRepository.ObtenerPlanPorIdAsync(planId);

                var detallesSuscripcion = SubscriptionMappers.MapPayPalSubscriptionToEntity(
                    subscriptionDetails,
                    plan);

                await _paypalService.SaveOrUpdateSubscriptionDetailsAsync(detallesSuscripcion);

                return detallesSuscripcion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear detalles de suscripción para planId {PlanId}", planId);
                throw;
            }
        }
    }
}
