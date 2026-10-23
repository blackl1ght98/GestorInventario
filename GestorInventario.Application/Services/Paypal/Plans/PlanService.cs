using GestorInventario.Application.Mappers;
using Microsoft.Extensions.Logging;
using System.Globalization;
using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Subscriptions;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.Plans;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Application.Services.Paypal.Plans
{
    public class PlanService : IPlanService
    {
        private readonly IPaypalRepository _paypalRepository;
        private readonly IPaypalSubscriptionService _paypalSubscriptionService;
        private readonly ILogger<PlanService> _logger;

        public PlanService(IPaypalRepository paypalRepository,IPaypalSubscriptionService subscriptionService, ILogger<PlanService> logger)
        {
            _paypalRepository = paypalRepository;
            _paypalSubscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> SavePlanDetailsAsync(string planId, PlanDetailsDto planDetails)
        {
            // Verificar si el plan ya existe
            var existingPlan = await _paypalRepository.ObtenerPlanPorIdAsync(planId);

            if (existingPlan != null)
            {
                _logger.LogInformation($"El plan con ID {planId} ya existe en la base de datos.");
                return OperationResult<string>.Fail();
            }

            // Mapeo y guardado
            var planDetail = PlanMapper.MapPayPalPlanToEntity(planId, planDetails);
            await _paypalRepository.AgregarPlanAsync(planDetail);
            _logger.LogInformation($"Detalles del plan {planId} guardados exitosamente.");
            return OperationResult<string>.Ok();
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

        public async Task SavePlanPriceUpdateAsync(PlanPriceUpdateResultDto planPriceUpdate)
        {
            if (planPriceUpdate == null)
                throw new ArgumentNullException(nameof(planPriceUpdate));

            var planId = planPriceUpdate.PlanId;

            var planDetail = await _paypalRepository.ObtenerPlanPorIdAsync(planId);
            if (planDetail == null)
            {
                _logger.LogWarning("No se encontró el plan con ID {PlanId} en la base de datos.", planId);
                throw new ArgumentException($"No se encontró el plan con ID {planId}.");
            }

            // Tiene trial si en BD hay intervalo de prueba
            bool hasTrial = !string.IsNullOrEmpty(planDetail.TrialIntervalUnit);

            foreach (var pricingScheme in planPriceUpdate.PricingSchemes)
            {
                int sequence = pricingScheme.BillingCycleSequence;
                string priceValue = pricingScheme.Value;

                if (string.IsNullOrEmpty(priceValue))
                {
                    _logger.LogWarning(
                        "El precio del ciclo {Sequence} del plan {PlanId} es nulo o vacío.",
                        sequence, planId);
                    continue;
                }

                if (!decimal.TryParse(
                        priceValue,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out decimal price))
                {
                    _logger.LogWarning(
                        "No se pudo parsear el precio '{Price}' del ciclo {Sequence} del plan {PlanId}.",
                        priceValue, sequence, planId);
                    continue;
                }

                if (hasTrial && sequence == 1)
                {
                    planDetail.TrialFixedPrice = price;
                    _logger.LogInformation(
                        "Precio trial del plan {PlanId} actualizado a {Price}.", planId, price);
                }
                else if ((hasTrial && sequence == 2) || (!hasTrial && sequence == 1))
                {
                    planDetail.RegularFixedPrice = price;
                    _logger.LogInformation(
                        "Precio regular del plan {PlanId} actualizado a {Price}.", planId, price);
                }
                else
                {
                    _logger.LogWarning(
                        "Ciclo de facturación {Sequence} no válido para el plan {PlanId} (hasTrial={HasTrial}).",
                        sequence, planId, hasTrial);
                }
            }

            await _paypalRepository.ActualizarPlanAsync(planDetail);

            _logger.LogInformation(
                "Precios del plan {PlanId} actualizados correctamente en la base de datos.", planId);
        }

        
        
        public async Task<OperationResult<string>> ActualizarPrecioPlanAsync(UpdatePlanPriceRequestDto request)
        {
             // Validar que el plan exista
                var plan = await _paypalRepository.ObtenerPlanPorIdAsync(request.PlanId);
                if (plan == null)
                {
                    return OperationResult<string>.Fail( $"No se encontró el plan con ID {request.PlanId}");  
                }

                // Obtener los detalles del plan desde PayPal para verificar la moneda
                var planDetails = await  _paypalSubscriptionService .ObtenerDetallesPlan(request.PlanId);
                string planCurrency = planDetails.Data.BillingCycles
                    ?.FirstOrDefault(c => c.PricingScheme?.FixedPrice?.CurrencyCode != null)
                    ?.PricingScheme.FixedPrice.CurrencyCode;
                if (string.IsNullOrEmpty(planCurrency))
                {
                    return OperationResult<string>.Fail( "No se pudo determinar la moneda del plan.");  
                  
                }

                // Validar que la moneda solicitada coincida con la del plan
                if (request.Currency != planCurrency)
                {
                    return OperationResult<string>.Fail(  $"La moneda {request.Currency} no coincide con la moneda del plan ({planCurrency}).");  
                   
                }

                // Llamar al servicio para actualizar el precio
                var result = await _paypalSubscriptionService.UpdatePricingPlanAsync(request.PlanId,
                    request.TrialAmount, request.RegularAmount, request.Currency);
                if (!result.Success)
                {
                    return OperationResult<string>.Fail(  $"No se ha podido actualizar el plan en este momento, intentelo de nuevo mas tarde");  

                   
                }

                await SavePlanPriceUpdateAsync(result.Data);
                return OperationResult<string>.Ok( "Precio del plan actualizado con éxito." );
              
        }
    }
}