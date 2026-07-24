using GestorInventario.Application.Mappers;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Paypal;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;
using log4net.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Application.Services.Paypal.Subscription
{
    public class SubscriptionService: ISubscriptionService
    {
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(IPaypalRepository paypalRepository, ILogger<SubscriptionService> logger)
        {
            _paypalRepository = paypalRepository;
            _logger = logger;
        }

        public async Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId)
        {
            try
            {
                var plan = await _paypalRepository.ObtenerPlanPorIdAsync(planId);

                var detallesSuscripcion = SubscriptionMappers.MapPayPalSubscriptionToEntity(
                    subscriptionDetails,
                    plan);

                await SaveOrUpdateSubscriptionDetailsAsync(detallesSuscripcion);

                return detallesSuscripcion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear detalles de suscripción para planId {PlanId}", planId);
                throw;
            }
        }
        private async Task SaveOrUpdateSubscriptionDetailsAsync(SubscriptionDetail subscriptionDetails)
        {
            var existingSubscription = await _paypalRepository
                .ObtenerSubscriptionIdAsync(subscriptionDetails.SubscriptionId);

            if (existingSubscription is null)
            {
                await _paypalRepository.AgregarDetallesSubscripcionAsync(subscriptionDetails);
                _logger.LogInformation("Suscripción creada: {SubscriptionId}",
                    subscriptionDetails.SubscriptionId);
                return;
            }

            // Si no hay cambios, salimos sin escribir en BD .
            var hasChanges = SubscriptionMappers.TryUpdateFromPayPal(
                existingSubscription, subscriptionDetails);
            if (!hasChanges)
            {
                _logger.LogDebug("Suscripción sin cambios: {SubscriptionId}",
                    subscriptionDetails.SubscriptionId);
                return;
            }

            await _paypalRepository.ActualizarDetallesSubscripcionAsync(existingSubscription);
            _logger.LogInformation("Suscripción actualizada: {SubscriptionId}",
                subscriptionDetails.SubscriptionId);
        }
        public async Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId)
        {

            // Verificar si la relación ya existe
            var existingRelation = await _paypalRepository.ObtenerSubscricionUsuarioAsync(userId, subscriptionId);

            if (existingRelation == null)
            {
                var userSubscription = new UserSubscription
                {
                    UserId = userId,
                    SubscriptionId = subscriptionId,
                    NombreSusbcriptor = subscriberName,
                    PaypalPlanId = planId
                };

                await _paypalRepository.AgregarSubscripcionUsuarioAsync(userSubscription);

                _logger.LogInformation($"Relación UserSubscription creada para usuario {userId}, suscripción {subscriptionId}");

            }

        }
        public async Task UpdateSubscriptionStatusAsync(string subscriptionId, string status)
        {

            var subscription = await _paypalRepository.ObtenerSubscriptionIdAsync(subscriptionId);

            if (subscription == null)
            {
                _logger.LogWarning($"No se encontró la suscripción con ID {subscriptionId}");
                return;
            }


            if (status == "ACTIVE" && subscription.Status != "CANCELLED" && subscription.Status != "SUSPENDED")
            {
                _logger.LogInformation($"No se puede activar la suscripción {subscriptionId} porque no está en estado CANCELLED o SUSPENDED (estado actual: {subscription.Status})");
                return;
            }

            else if (status == "SUSPENDED" && subscription.Status != "ACTIVE")
            {
                _logger.LogInformation($"No se puede suspender la suscripción {subscriptionId} porque no está en estado ACTIVE (estado actual: {subscription.Status})");
                return;
            }

            else if (status == "CANCELLED" && subscription.Status != "ACTIVE" && subscription.Status != "SUSPEND")
            {
                _logger.LogInformation($"No se puede cancelar la suscripción {subscriptionId} porque no está en estado ACTIVE o SUSPEND (estado actual: {subscription.Status})");
                return;
            }

            subscription.Status = status;
            await _paypalRepository.ActualizarDetallesSubscripcionAsync(subscription);

            _logger.LogInformation($"Estado de la suscripción {subscriptionId} actualizado a {status}");


        }
    }
}
