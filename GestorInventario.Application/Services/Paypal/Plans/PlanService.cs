using GestorInventario.Application.Mappers;
using GestorInventario.Interfaces.Application.Services.Paypal;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Paypal.Responses.POST.Subscription;
using log4net.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GestorInventario.Application.Services.Paypal.Plans
{
    public class PlanService: IPlanService
    {
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<PlanService> _logger;

        public PlanService(IPaypalRepository paypalRepository, ILogger<PlanService> logger)
        {
            _paypalRepository = paypalRepository;
            _logger = logger;
        }

        public async Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails)
        {
            // Verificar si el plan ya existe
            var existingPlan = await _paypalRepository.ObtenerPlanPorIdAsync(planId);

            if (existingPlan != null)
            {
                _logger.LogInformation($"El plan con ID {planId} ya existe en la base de datos.");
                return;
            }
            // Mapeo y guardado
            var planDetail = PlanMapper.MapPayPalPlanToEntity(planId, planDetails);
            await _paypalRepository.AgregarPlanAsync(planDetail);
            _logger.LogInformation($"Detalles del plan {planId} guardados exitosamente.");
        }
        public async Task UpdatePlanStatusAsync(string planId, string status)
        {
            var planDetails = await _paypalRepository.ObtenerPlanPorIdAsync(planId);
            if (planDetails != null)
            {
                planDetails.Status = status;
                await _paypalRepository.ActualizarPlanAsync(planDetails);

            }
        }
        public async Task SavePlanPriceUpdateAsync(string planId, UpdatePricingPlanDto planPriceUpdate)
        {

            // Buscar el plan en la base de datos
            var planDetail = await _paypalRepository.ObtenerPlanPorIdAsync(planId);
            if (planDetail == null)
            {
                _logger.LogWarning($"No se encontró el plan con ID {planId} en la base de datos.");
                throw new ArgumentException($"No se encontró el plan con ID {planId}.");
            }

            // Verificar si el plan tiene un ciclo de prueba (basado en los campos Trial no nulos)
            bool hasTrial = !string.IsNullOrEmpty(planDetail.TrialIntervalUnit);

            // Procesar los esquemas de precios
            foreach (var pricingScheme in planPriceUpdate.PricingSchemes)
            {
                int billingCycleSequence = pricingScheme.BillingCycleSequence;
                string priceValue = pricingScheme.PricingScheme.FixedPrice.Value;

                if (string.IsNullOrEmpty(priceValue))
                {
                    _logger.LogWarning($"El precio proporcionado para el ciclo {billingCycleSequence} del plan {planId} es nulo o vacío.");
                    continue;
                }

                decimal price = decimal.Parse(priceValue, CultureInfo.InvariantCulture);

                if (hasTrial && billingCycleSequence == 1)
                {
                    // Actualizar el precio del ciclo de prueba
                    planDetail.TrialFixedPrice = price;
                    _logger.LogInformation($"Precio del ciclo de prueba para el plan {planId} actualizado a {price}.");
                }
                else if (hasTrial && billingCycleSequence == 2 || !hasTrial && billingCycleSequence == 1)
                {
                    // Actualizar el precio del ciclo regular
                    planDetail.RegularFixedPrice = price;
                    _logger.LogInformation($"Precio del ciclo regular para el plan {planId} actualizado a {price}.");
                }
                else
                {
                    _logger.LogWarning($"Ciclo de facturación {billingCycleSequence} no válido para el plan {planId}.");
                }
            }
            await _paypalRepository.ActualizarPlanAsync(planDetail);

            _logger.LogInformation($"Precios del plan {planId} actualizados exitosamente en la base de datos.");

        }
    }
}
