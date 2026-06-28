using GestorInventario.Application.DTOs;

using GestorInventario.Application.Mappers;
using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;

using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Email;
using GestorInventario.Shared.DTOS.Paypal.Responses.POST.Subscription;
using GestorInventario.Shared.Utilities;
using GestorInventario.Utilities;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace GestorInventario.Application.Services.Common
{
    public class PaypalService: IPaypalService
    {
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<PaypalService> _logger;
        private readonly IPedidoRepository _pedidoRepository;  
        private readonly IEmailService _emailService;
        public PaypalService(IPaypalRepository paypalRepository, ILogger<PaypalService> logger, IPedidoRepository pedido,  
            IEmailService email)
        {
            _paypalRepository = paypalRepository;
            _logger = logger;
            _pedidoRepository = pedido;             
            _emailService = email;
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
        public async Task SaveOrUpdateSubscriptionDetailsAsync(SubscriptionDetail subscriptionDetails)
        {
            var existingSubscription = await _paypalRepository.ObtenerSubscriptionIdAsync(subscriptionDetails.SubscriptionId);

            if (existingSubscription != null)
            {
                bool updated = SubscriptionMappers.TryUpdateFromPayPal(existingSubscription, subscriptionDetails);

                if (updated)
                {
                    await _paypalRepository.ActualizarDetallesSubscripcionAsync(existingSubscription);
                    _logger.LogInformation("Suscripción actualizada: {SubscriptionId}", subscriptionDetails.SubscriptionId);
                }
            }
            else
            {
                await _paypalRepository.AgregarDetallesSubscripcionAsync(subscriptionDetails);
                _logger.LogInformation("Suscripción creada: {SubscriptionId}", subscriptionDetails.SubscriptionId);
            }
        }

        public async Task SaveUserSubscriptionAsync(int userId, string subscriptionId, string subscriberName, string planId)
        {
            
                // Verificar si la relación ya existe
                var existingRelation = await _paypalRepository.ObtenerSubscricionUsuarioAsync(userId,subscriptionId);

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
        public async Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo)
        {
            try
            {
                var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el pedido con ID {PedidoId}", pedidoId);
                    return OperationResult<string>.Fail("Pedido no encontrado");
                }

                var usuarioPedido = pedido.IdUsuarioNavigation?.Email ?? "Email no disponible";
                var nombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto ?? "Cliente";

                var productosConDetalles = pedido.DetallePedidos?
                    .Select(detalle => new PayPalPaymentItem
                    {
                        ItemName = detalle.Producto?.NombreProducto ?? "N/A",
                        ItemQuantity = detalle.Cantidad,
                        ItemPrice = detalle.Producto?.Precio ?? 0,
                        ItemCurrency = pedido.Currency,
                        ItemSku = detalle.Producto?.Descripcion ?? "N/A"
                    })
                    .ToList() ?? new List<PayPalPaymentItem>();

                var correo = new EmailReembolsoAprobadoDto
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = nombreCliente,
                    EmailCliente = usuarioPedido,
                    FechaRembolso = DateTime.UtcNow,
                    CantidadADevolver = montoReembolsado,
                    MotivoRembolso = motivo,
                    Productos = productosConDetalles
                };

                await _emailService.EnviarNotificacionReembolsoAsync(correo);

                return OperationResult<string>.Ok("Correo de notificación de reembolso enviado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación de reembolso para el pedido ID {PedidoId}", pedidoId);
                return OperationResult<string>.Fail("Error al enviar el correo");
            }
        }

    }
}
