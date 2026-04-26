using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using System.Globalization;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class PaypalService: IPaypalService
    {
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<PaypalService> _logger;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        public PaypalService(IPaypalRepository paypalRepository, ILogger<PaypalService> logger, IPedidoRepository pedido, IEmailService email, ICurrentUserAccessor currentUserAccessor)
        {
            _paypalRepository = paypalRepository;
            _logger = logger;
            _pedidoRepository = pedido;
            _emailService = email;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails)
        {
           
                // Verificar si el plan ya existe
                var existingPlan = await _paypalRepository.ObtenerPlanPorIdAsync(planId);

                if (existingPlan != null)
                {
                    _logger.LogInformation($"El plan con ID {planId} ya existe en la base de datos.");
                    return;   // Sale de la transacción sin hacer nada más
                }

                // Mapeo y guardado
                var planDetail = MapToPlanDetail(planId, planDetails);
                await _paypalRepository.AgregarPlanAsync(planDetail);

                _logger.LogInformation($"Detalles del plan {planId} guardados exitosamente.");
           
        }
        private PlanDetail MapToPlanDetail(string planId, PaypalPlanDetailsDto planDetails)
        {
            var planDetail = new PlanDetail
            {
                Id = Guid.NewGuid().ToString(),
                PaypalPlanId = planId,
                ProductId = planDetails.ProductId,
                Name = planDetails.Name,
                Description = planDetails.Description,
                Status = planDetails.Status,
                AutoBillOutstanding = planDetails.PaymentPreferences.AutoBillOutstanding,
                SetupFee = planDetails.PaymentPreferences.SetupFee?.Value != null
                    ? decimal.Parse(planDetails.PaymentPreferences.SetupFee.Value, CultureInfo.InvariantCulture)
                    : 0,
                SetupFeeFailureAction = planDetails.PaymentPreferences.SetupFeeFailureAction,
                PaymentFailureThreshold = planDetails.PaymentPreferences.PaymentFailureThreshold,
                TaxPercentage = planDetails.Taxes?.Percentage != null
                    ? decimal.Parse(planDetails.Taxes.Percentage, CultureInfo.InvariantCulture)
                    : 0,
                TaxInclusive = planDetails.Taxes?.Inclusive ?? false
            };

            // Manejo de ciclos de facturación
            if (planDetails.BillingCycles.Length > 1)
            {
                planDetail.TrialIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                planDetail.TrialIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                planDetail.TrialTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                planDetail.TrialFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                    ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                    : 0;

                planDetail.RegularIntervalUnit = planDetails.BillingCycles[1].Frequency.IntervalUnit;
                planDetail.RegularIntervalCount = planDetails.BillingCycles[1].Frequency.IntervalCount;
                planDetail.RegularTotalCycles = planDetails.BillingCycles[1].TotalCycles;
                planDetail.RegularFixedPrice = planDetails.BillingCycles[1].PricingScheme.FixedPrice?.Value != null
                    ? decimal.Parse(planDetails.BillingCycles[1].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                    : 0;
            }
            else if (planDetails.BillingCycles.Length == 1)
            {
                planDetail.RegularIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                planDetail.RegularIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                planDetail.RegularTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                planDetail.RegularFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                    ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                    : 0;
            }

            return planDetail;
        }
        public async Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo)
        {
            try
            {
                var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);

                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el pedido con ID {PedidoId}", pedidoId);
                    return OperationResult<string>.Fail("Pedido no encontrado");
                }

                var usuarioPedido = pedido.IdUsuarioNavigation?.Email ?? "Email no disponible";
                var nombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto ?? "Cliente";

                // Extraer la lista de productos con detalles
                var productosConDetalles = pedido.DetallePedidos?
                    .Select(detalle => new PayPalPaymentItem
                    {
                        ItemName = detalle.Producto?.NombreProducto ?? "N/A",
                        ItemQuantity = detalle.Cantidad ?? 0,
                        ItemPrice = detalle.Producto?.Precio ?? 0,
                        ItemCurrency = pedido.Currency ?? "USD",
                        ItemSku = detalle.Producto?.Descripcion ?? "N/A"
                    })
                    .ToList() ?? new List<PayPalPaymentItem>();

                // Crear DTO para el correo
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

                // Enviar el correo
                await _emailService.EnviarNotificacionReembolsoAsync(correo);
                return OperationResult<string>.Ok("Correo de notificación de reembolso enviado correctamente");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación de reembolso para el pedido ID {PedidoId}", pedidoId);
                return OperationResult<string>.Fail("Error al enviar el correo");

            }
        }
        public async Task UpdatePedidoStatusAsync(int pedidoId, string status, string refundId, string estadoVenta)
        {
           
                var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                pedido.EstadoPedido = status;
                pedido.RefundId = refundId;

                await _pedidoRepository.ActualizarPedidoAsync(pedido);

                var usuarioActual = _currentUserAccessor.GetCurrentUserId();

                // Crear o actualizar registro de reembolso
                var obtenerRembolso = await _paypalRepository.ObtenRembolsoAsync(pedido.NumeroPedido);

                if (obtenerRembolso == null)
                {
                    var rembolso = new Rembolso
                    {

                        NumeroPedido = pedido.NumeroPedido,
                        NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                        EmailCliente = pedido.IdUsuarioNavigation?.Email,
                        FechaRembolso = DateTime.UtcNow,
                        MotivoRembolso = "Rembolso solicitado por el usuario",
                        EstadoRembolso = "REMBOLSO APROVADO",
                        RembosoRealizado = true,
                        UsuarioId = usuarioActual,
                        PedidoId = pedido.Id,
                        EstadoVenta = estadoVenta
                    };
                    await _paypalRepository.AgregarRembolsoAsync(rembolso);
                }
                else
                {
                    obtenerRembolso.EstadoRembolso = "REMBOLSO APROVADO";
                    obtenerRembolso.RembosoRealizado = true;
                    obtenerRembolso.EstadoVenta = estadoVenta;
                    obtenerRembolso.FechaRembolso = DateTime.UtcNow;


                    await _paypalRepository.ActualizarRembolsoAsync(obtenerRembolso);
                }


                _logger.LogInformation($"Estado del pedido {pedidoId} actualizado a {status}");
            

        }
        public async Task RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string refundId, decimal montoReembolsado, string motivo, string estadoVenta)
        {

            // Obtener el pedido con los datos relacionados
            var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);

                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                // Obtener el detalle específico por ID
                var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
                if (detalleReembolsado == null)
                    throw new ArgumentException($"Detalle con ID {detalleId} no encontrado.");

                // Evitar reembolsos duplicados
                if (detalleReembolsado.Rembolsado ?? false)
                    throw new InvalidOperationException($"El detalle con ID {detalleId} ya ha sido reembolsado.");

                var usuarioActual = _currentUserAccessor.GetCurrentUserId();

                // Crear registro de reembolso
                var rembolso = new Rembolso
                {
                    PedidoId = pedido.Id,
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                    EmailCliente = pedido.IdUsuarioNavigation?.Email,
                    FechaRembolso = DateTime.UtcNow,
                    MotivoRembolso = motivo,
                    EstadoRembolso = "REMBOLSO PARACIAL APROVADO",
                    RembosoRealizado = true,
                    UsuarioId = usuarioActual,
                    EstadoVenta = estadoVenta

                };

                await _paypalRepository.AgregarRembolsoAsync(rembolso);

                // Marcar el detalle correcto como reembolsado
                detalleReembolsado.Rembolsado = true;
                await _pedidoRepository.ActualizarDetallePedidoAsync(detalleReembolsado);

                _logger.LogInformation($"Reembolso registrado para pedido {pedidoId}, detalle {detalleId}.");
           
        }
        public async Task AddInfoTrackingOrder(int pedidoId, string tracking, string url, string carrier)
        {
           
                var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(pedidoId);
                if (pedido == null)
                    throw new ArgumentException($"Pedido con ID {pedidoId} no encontrado.");

                pedido.TrackingNumber = tracking;
                pedido.UrlTracking = url;
                pedido.Transportista = carrier;
                await _pedidoRepository.ActualizarPedidoAsync(pedido);
           

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
                    else if ((hasTrial && billingCycleSequence == 2) || (!hasTrial && billingCycleSequence == 1))
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
           
                // Verificar si la suscripción ya existe
                var existingSubscription = await _paypalRepository.ObtenerSubscriptionIdAsync(subscriptionDetails.SubscriptionId);

                if (existingSubscription != null)
                {
                    // Comparar los detalles para determinar si hay cambios
                    bool hasChanges = !(
                        existingSubscription.PlanId == subscriptionDetails.PlanId &&
                        existingSubscription.Status == subscriptionDetails.Status &&
                        existingSubscription.StartTime == subscriptionDetails.StartTime &&
                        existingSubscription.StatusUpdateTime == subscriptionDetails.StatusUpdateTime &&
                        existingSubscription.SubscriberName == subscriptionDetails.SubscriberName &&
                        existingSubscription.SubscriberEmail == subscriptionDetails.SubscriberEmail &&
                        existingSubscription.PayerId == subscriptionDetails.PayerId &&
                        existingSubscription.OutstandingBalance == subscriptionDetails.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == subscriptionDetails.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == subscriptionDetails.NextBillingTime &&
                        existingSubscription.LastPaymentTime == subscriptionDetails.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == subscriptionDetails.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == subscriptionDetails.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == subscriptionDetails.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == subscriptionDetails.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == subscriptionDetails.CyclesRemaining &&
                        existingSubscription.TotalCycles == subscriptionDetails.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == subscriptionDetails.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == subscriptionDetails.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == subscriptionDetails.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == subscriptionDetails.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                        await _paypalRepository.ActualizarDetallesSubscripcionAsync(subscriptionDetails);
                        _logger.LogInformation($"Suscripción actualizada: {subscriptionDetails.SubscriptionId}");
                    }
                }
                else
                {
                    await _paypalRepository.AgregarDetallesSubscripcionAsync(subscriptionDetails);
                    _logger.LogInformation($"Suscripción creada: {subscriptionDetails.SubscriptionId}");
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

    }
}
