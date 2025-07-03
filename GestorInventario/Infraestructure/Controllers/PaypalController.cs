using AutoMapper;
using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Domain.Models.ViewModels.Paypal;
using GestorInventario.Domain.Models.ViewModels.Paypal.GestorInventario.Domain.Models.ViewModels.Paypal;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaypalController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerarPaginas _generarPaginas;
     
        private readonly ILogger<PaypalController> _logger;
        private readonly IPaypalController _paypalController;
        private readonly ICarritoRepository _carritoRepository;
        private readonly IMapper _mapper;
        private readonly PolicyExecutor _policyExecutor;
        public PaypalController(GestorInventarioContext context, IUnitOfWork unit, GenerarPaginas generar,  ILogger<PaypalController> logger, 
            IPaypalController paypalController, ICarritoRepository carritoRepository, IMapper map, PolicyExecutor executor)
        {
            _context = context;
            _unitOfWork = unit;
            _generarPaginas = generar;
         _policyExecutor=executor;
            _logger = logger;
            _paypalController = paypalController;
            _carritoRepository = carritoRepository;
            _mapper = map;
        }

        //Metodo que muestra los productos creados en paypal
        public async Task<IActionResult> MostrarProductos(int pagina = 1)
        {
            // Establece el número de productos por página.
            int pageSize = 6;

          
            var (productsResponse, hasNextPage) = await _unitOfWork.PaypalService.GetProductsAsync(pagina, pageSize);

            // Deserializa la respuesta JSON del servidor.
            dynamic productsJson = JsonConvert.DeserializeObject(productsResponse);

            // Crea una lista para almacenar los productos mapeados al modelo ProductoViewModel.
            var productos = new List<ProductoViewModel>();

            // Recorre el array de productos en la respuesta JSON.
            foreach (var product in productsJson.products)
            {
                // Mapea cada producto al modelo ProductoViewModel y lo agrega a la lista productos.
                productos.Add(new ProductoViewModel
                {
                    id = product.id,
                    name = product.name,
                    description = product.description,
                });
            }

            // Construye el objeto Paginacion con los valores adecuados para la paginacion de paypal.
            var paginacion = new Paginacion
            {
                TotalPaginas = hasNextPage ? pagina + 1 : pagina,
                PaginaActual = pagina,
                Radio = 3 
            };

            // Genera las páginas 
            var paginacionMetodo = new PaginacionMetodo();
            var paginas = paginacionMetodo.GenerarListaPaginas(paginacion);

            // Crea un modelo que contiene los productos paginados y la información de la paginación.
            var model = new ProductosPaginadosViewModel
            {
                Productos = productos,
                Paginas = paginas,
                PaginaActual = pagina,
                TienePaginaSiguiente = hasNextPage,
                TienePaginaAnterior = pagina > 1
            };

          
            return View(model);
        }
       
        
        public async Task<IActionResult> MostrarPlanes([FromQuery] int pagina = 1, [FromQuery] int cantidadAMostrar = 6)
        {
            try
            {
               
                _logger.LogInformation("Página solicitada: {Pagina}, CantidadAMostrar: {Cantidad}", pagina, cantidadAMostrar);

               
                var (plans, hasNextPage) = await _unitOfWork.PaypalService.GetSubscriptionPlansAsync(pagina, cantidadAMostrar);

                // Mapear los planes a PlanesViewModel
                var planesViewModel = _mapper.Map<List<PlanesViewModel>>(plans);

                // Calcular TotalPaginas basado en hasNextPage
                int totalPaginas = hasNextPage ? pagina + 1 : pagina;

                // Crear objeto Paginacion
                var paginacion = new Paginacion
                {
                    Pagina = pagina,
                    CantidadAMostrar = cantidadAMostrar,
                    TotalPaginas = totalPaginas, 
                    PaginaActual = pagina,
                    Radio = 3
                };

                // Generar los enlaces de paginación
                var paginacionMetodo = new PaginacionMetodo();
                var paginas = paginacionMetodo.GenerarListaPaginas(paginacion);

                // Crear el modelo para la vista
                var model = new PlanesPaginadosViewModel
                {
                    Planes = planesViewModel,
                    Paginas = paginas,
                    PaginaActual = pagina,
                    TienePaginaSiguiente = hasNextPage,
                    TienePaginaAnterior = pagina > 1,
                    CantidadAMostrar = cantidadAMostrar 
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "Error al obtener los planes de suscripción. Intenta de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los planes de suscripción para la página {Pagina}", pagina);
                return RedirectToAction("Error", "Home");
            }
        }
        // Método para crear la lista de categorías a partir de la enumeración
        private List<string> GetCategoriesFromEnum()
        {
            return Enum.GetNames(typeof(Category)).ToList();
        }
        //Metodo que obtiene los datos necesarios antes de crear el producto al que se suscribira
        public async Task<IActionResult> CrearProducto()
        {
          
            var model = new ProductViewModelPaypal();
            ViewData["Moneda"] = new SelectList(await _policyExecutor.ExecutePolicyAsync(() => _carritoRepository.ObtenerMoneda()), "Codigo", "Codigo");
            // Obtener las categorías desde la enumeración y asignarlas al modelo
            model.Categories = GetCategoriesFromEnum();

            return View(model);
        }

      //Metodo que crea el producto al que se suscribira el usuario
        [HttpPost]
        public async Task<IActionResult> CrearProducto(ProductViewModelPaypal model,string monedaSeleccionada)
        {
            // Cambia la cultura actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            if (ModelState.IsValid)
            {
                try
                {
                    // Crear un producto usando los datos del formulario
                    var productResponse = await _unitOfWork.CreateProductAndNotifyAsync(model.Name, model.Description, model.Type, model.Category);
                    dynamic productJson = JsonConvert.DeserializeObject(productResponse);
                    string productId = productJson.id;
                    ViewData["Moneda"] = new SelectList(await _carritoRepository.ObtenerMoneda(), "Codigo", "Codigo");
                    // Crear el plan de suscripción
                    string planResponse = await _unitOfWork.PaypalService.CreateSubscriptionPlanAsync(
                        productId,
                        model.PlanName,
                        model.PlanDescription,
                        model.Amount,
                        monedaSeleccionada,
                        model.HasTrialPeriod ? model.TrialPeriodDays : 0, // Pasamos los días de prueba
                        model.HasTrialPeriod ? model.TrialAmount : 0.00m   // Pasamos el costo del periodo de prueba
                    );

                   
                    if (planResponse.Contains("\"error\""))
                    {
                        return StatusCode(500, $"Error al crear el plan de suscripción: {planResponse}");
                    }

                    return RedirectToAction(nameof(MostrarProductos));
                }
                catch (Exception ex)
                {
                 
                    return StatusCode(500, $"Error inesperado: {ex.Message}");
                }
            }

           
            model.Categories = GetCategoriesFromEnum();
            return View(model);
        }

        //Metodo para desactivar el plan
        [HttpPost]
        public async Task<IActionResult> DesactivarPlan(string productId, string planId)
        {
            try
            {
                var activeSubscriptions= await _paypalController.ObtenerSuscriptcionesActivas(planId);              
                var userSubscriptions = await _paypalController.SusbcripcionesUsuario(planId);
                if (activeSubscriptions.Any() || userSubscriptions.Any())
                {
                    return StatusCode(400, "No se puede cancelar el plan porque hay suscriptores activos.");
                }
                var deleteResponse = await _unitOfWork.PaypalService.DesactivarPlan(productId, planId);

             
                return RedirectToAction(nameof(MostrarProductos), new { mensaje = deleteResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }

        //Metodo para desactivar el producto
        [HttpPost]
        public async Task<IActionResult> DesactivarProducto(string id)
        {
            try
            {
            
                var deleteResponse = await _unitOfWork.PaypalService.MarcarDesactivadoProducto(id);

                
                return RedirectToAction(nameof(MostrarProductos), new { mensaje = deleteResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar el producto y el plan: {ex.Message}");
            }
        }
        //Metodo que muestra la vista para editar
        public IActionResult EditarProductoPaypal()
        {
            return View();
        }
        //Metodo que edita el producto de paypal
        [HttpPost]
        public async Task<IActionResult> EditarProductoPaypal( string id, EditProductPaypal model)
        {

            if (ModelState.IsValid)
            {

                try
                {
                    var authToken = await _unitOfWork.PaypalService.GetAccessTokenAsync();
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                        var productResponse1 = await httpClient.GetAsync($"https://api-m.sandbox.paypal.com/v1/catalogs/products/{id}");
                        if (!productResponse1.IsSuccessStatusCode)
                        {
                            var errorContent = await productResponse1.Content.ReadAsStringAsync();
                            throw new Exception($"No se pudo encontrar el producto con ID {id}: {productResponse1.StatusCode} - {errorContent}");
                        }
                    }
                        
                        var productResponse = await _unitOfWork.PaypalService.EditarProducto(id,model.name,model.description );
                    return RedirectToAction(nameof(MostrarProductos));
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error al crear el plan de suscripción: {ex.Message}");
                }
            }

        

            return View(model);
        }
        //Metodo que inicia el proceso de suscripcion
        [HttpPost]
        public async Task<IActionResult> IniciarSuscripcion( string plan_id, string planName)
        {
            // Define las URLs de retorno y cancelación
            string returnUrl = Url.Action("ConfirmarSuscripcion", "Paypal", null, Request.Scheme);
            string cancelUrl = Url.Action("CancelarSuscripcion", "Paypal", null, Request.Scheme);      
            try
            {
                
                string approvalUrl = await _unitOfWork.PaypalService.Subscribirse(plan_id, returnUrl, cancelUrl, planName);

             
                return Redirect(approvalUrl);
            }
            catch (Exception ex)
            {
           
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
      
        public async Task<IActionResult> ConfirmarSuscripcion(string subscription_id, string token, string ba_token)
        {
            try
            {
                if (string.IsNullOrEmpty(subscription_id) || string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "No se pudo confirmar la suscripción. Faltan parámetros requeridos.";
                    return RedirectToAction("Error", "Home");
                }

                var existeUsuario = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;

                if (!int.TryParse(existeUsuario, out usuarioId))
                {
                    TempData["ErrorMessage"] = "No se pudo identificar al usuario.";
                    return RedirectToAction("Error", "Home");
                }

            
                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.PaypalService.ObtenerDetallesSuscripcion(subscription_id));

                if (subscriptionDetails == null)
                {
                    TempData["ErrorMessage"] = "No se pudieron obtener los detalles de la suscripción desde PayPal.";
                    return RedirectToAction("Error", "Home");
                }

                // Convertir plan_id a string
                string planId = (string)subscriptionDetails.plan_id;

            
                var plan = await _policyExecutor.ExecutePolicyAsync(() => _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId));

                if (plan == null)
                {
                    await _policyExecutor.ExecutePolicy(() => _paypalController.DetallesSubscripcion(planId));
                    plan = await _policyExecutor.ExecutePolicyAsync(() => _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId));
                }

                // Establecer la fecha mínima SQL
                var minSqlDate = new DateTime(1753, 1, 1);

             
                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.id ?? string.Empty,
                    PlanId = subscriptionDetails.plan_id ?? string.Empty,
                    Status = subscriptionDetails.status ?? string.Empty,
                    StartTime = subscriptionDetails.start_time ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.status_updated_time ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.subscriber.name.given_name ?? string.Empty} {subscriptionDetails.subscriber.name.surname ?? string.Empty}",
                    SubscriberEmail = subscriptionDetails.subscriber.email_address ?? string.Empty,
                    PayerId = subscriptionDetails.subscriber.payer_id ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.billing_info.outstanding_balance.value != null ? Convert.ToDecimal(subscriptionDetails.billing_info.outstanding_balance.value) : 0,
                    OutstandingCurrency = subscriptionDetails.billing_info.outstanding_balance.currency_code ?? string.Empty,
                    NextBillingTime = subscriptionDetails.billing_info.next_billing_time ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.billing_info.last_payment?.time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.billing_info.last_payment?.amount.value != null ? Convert.ToDecimal(subscriptionDetails.billing_info.last_payment.amount.value) : 0,
                    LastPaymentCurrency = subscriptionDetails.billing_info.last_payment?.amount.currency_code ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.billing_info.final_payment_time ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.billing_info.cycle_executions[0]?.cycles_completed ?? 0,
                    CyclesRemaining = subscriptionDetails.billing_info.cycle_executions[0]?.cycles_remaining ?? 0,
                    TotalCycles = subscriptionDetails.billing_info.cycle_executions[0]?.total_cycles ?? 0,
                    TrialIntervalUnit = plan?.TrialIntervalUnit,
                    TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan?.TrialFixedPrice ?? 0
                };

                // Calcular la fecha del próximo pago después del período de prueba
                if (detallesSuscripcion.Status == "ACTIVE" && detallesSuscripcion.CyclesCompleted == 1 && detallesSuscripcion.CyclesRemaining == 0)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays((double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);
                }

                // Verificar si la suscripción ya existe en la base de datos
                var existingSubscription = await _policyExecutor.ExecutePolicyAsync(() => _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == detallesSuscripcion.SubscriptionId));

                if (existingSubscription != null)
                {
                    // Comparar los detalles y actualizar solo si han cambiado
                    bool hasChanges = !(
                        existingSubscription.PlanId == detallesSuscripcion.PlanId &&
                        existingSubscription.Status == detallesSuscripcion.Status &&
                        existingSubscription.StartTime == detallesSuscripcion.StartTime &&
                        existingSubscription.StatusUpdateTime == detallesSuscripcion.StatusUpdateTime &&
                        existingSubscription.SubscriberName == detallesSuscripcion.SubscriberName &&
                        existingSubscription.SubscriberEmail == detallesSuscripcion.SubscriberEmail &&
                        existingSubscription.PayerId == detallesSuscripcion.PayerId &&
                        existingSubscription.OutstandingBalance == detallesSuscripcion.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == detallesSuscripcion.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == detallesSuscripcion.NextBillingTime &&
                        existingSubscription.LastPaymentTime == detallesSuscripcion.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == detallesSuscripcion.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == detallesSuscripcion.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == detallesSuscripcion.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == detallesSuscripcion.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == detallesSuscripcion.CyclesRemaining &&
                        existingSubscription.TotalCycles == detallesSuscripcion.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == detallesSuscripcion.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == detallesSuscripcion.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == detallesSuscripcion.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == detallesSuscripcion.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                        _context.SubscriptionDetails.Update(detallesSuscripcion);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    _context.SubscriptionDetails.Add(detallesSuscripcion);
                    await _context.SaveChangesAsync();
                }

                // Verificar si ya existe una relación en UserSubscriptions
                var existeRelacion = await _policyExecutor.ExecutePolicyAsync(() => _context.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.UserId == usuarioId && us.SubscriptionId == subscription_id));

                if (existeRelacion == null)
                {
                    // Crear la relación en UserSubscriptions
                    var userSubscription = new UserSubscription
                    {
                        UserId = usuarioId,
                        SubscriptionId = subscription_id,
                        NombreSusbcriptor = detallesSuscripcion.SubscriberName,
                        PaypalPlanId = detallesSuscripcion.PlanId
                    };

                    _context.UserSubscriptions.Add(userSubscription);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "¡Suscripción confirmada con éxito!";
                return RedirectToAction("DetallesSuscripcion", new { id = subscription_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar la suscripción con ID {SubscriptionId}", subscription_id);
                TempData["ErrorMessage"] = $"Error al confirmar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpPost]
        //Metodo que cancela la suscripcion
        public async Task<IActionResult> CancelarSuscripcion([FromBody] PaypayRequest request)
        {
          
        
            try
            {
                
                string result = await _unitOfWork.PaypalService.CancelarSuscripcion(request.subscription_id, "No satisfecho");
                
                var actualizar = await _paypalController.ObtenerSubscripcion(request.subscription_id);
                if (actualizar != null)
                {
                    if (actualizar.Status == "ACTIVE" || actualizar.Status == "SUSPEND")
                    {
                        actualizar.Status = "CANCELLED";
                        await _context.UpdateEntityAsync(actualizar);
                    }
                }
                
                return RedirectToAction(nameof(TodasSuscripciones)); 
            }
            catch (Exception ex)
            {
               
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
      
        public async Task<IActionResult> DetallesSuscripcion(string id)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                // Obtener detalles de la suscripción desde PayPal
                var subscriptionDetails = await _policyExecutor.ExecutePolicyAsync(() => _unitOfWork.PaypalService.ObtenerDetallesSuscripcion(id)); 

                // Convertir plan_id a string para evitar problemas con árboles de expresión
                string planId = (string)subscriptionDetails.plan_id;

                // Obtener los detalles del plan desde la base de datos usando el PlanId
                var plan = await _policyExecutor.ExecutePolicyAsync(()=> _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId)); 

                if (plan == null)
                {
                    await _policyExecutor.ExecutePolicy(() => _paypalController.DetallesSubscripcion(planId));
                }

                // Establecer la fecha mínima SQL
                var minSqlDate = new DateTime(1753, 1, 1);

                // Crear el objeto de detalles de la suscripción
                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.id ?? string.Empty,
                    PlanId = subscriptionDetails.plan_id ?? string.Empty,
                    Status = subscriptionDetails.status ?? string.Empty,
                    StartTime = subscriptionDetails.start_time ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.status_updated_time ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.subscriber.name.given_name ?? string.Empty} {subscriptionDetails.subscriber.name.surname ?? string.Empty}",
                    SubscriberEmail = subscriptionDetails.subscriber.email_address ?? string.Empty,
                    PayerId = subscriptionDetails.subscriber.payer_id ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.billing_info.outstanding_balance.value != null ? Convert.ToDecimal(subscriptionDetails.billing_info.outstanding_balance.value) : 0,
                    OutstandingCurrency = subscriptionDetails.billing_info.outstanding_balance.currency_code ?? string.Empty,
                    NextBillingTime = subscriptionDetails.billing_info.next_billing_time ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.billing_info.last_payment?.time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.billing_info.last_payment?.amount.value != null ? Convert.ToDecimal(subscriptionDetails.billing_info.last_payment.amount.value) : 0,
                    LastPaymentCurrency = subscriptionDetails.billing_info.last_payment?.amount.currency_code ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.billing_info.final_payment_time ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.billing_info.cycle_executions[0]?.cycles_completed ?? 0,
                    CyclesRemaining = subscriptionDetails.billing_info.cycle_executions[0]?.cycles_remaining ?? 0,
                    TotalCycles = subscriptionDetails.billing_info.cycle_executions[0]?.total_cycles ?? 0,

                    // Asignar los valores del plan relacionados con el período de prueba
                    TrialIntervalUnit = plan.TrialIntervalUnit,
                    TrialIntervalCount = plan.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan.TrialFixedPrice ?? 0
                };

                // Calcular la fecha del próximo pago después del período de prueba
                if (detallesSuscripcion.Status == "ACTIVE" && detallesSuscripcion.CyclesCompleted == 1 && detallesSuscripcion.CyclesRemaining == 0)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays((double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);
                }

                // Verificar si la suscripción ya existe en la base de datos
                var existingSubscription = await _policyExecutor.ExecutePolicyAsync(() => _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == detallesSuscripcion.SubscriptionId)); 

                if (existingSubscription != null)
                {
                    // Comparar los detalles y actualizar solo si han cambiado
                    bool hasChanges = !(
                        existingSubscription.PlanId == detallesSuscripcion.PlanId &&
                        existingSubscription.Status == detallesSuscripcion.Status &&
                        existingSubscription.StartTime == detallesSuscripcion.StartTime &&
                        existingSubscription.StatusUpdateTime == detallesSuscripcion.StatusUpdateTime &&
                        existingSubscription.SubscriberName == detallesSuscripcion.SubscriberName &&
                        existingSubscription.SubscriberEmail == detallesSuscripcion.SubscriberEmail &&
                        existingSubscription.PayerId == detallesSuscripcion.PayerId &&
                        existingSubscription.OutstandingBalance == detallesSuscripcion.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == detallesSuscripcion.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == detallesSuscripcion.NextBillingTime &&
                        existingSubscription.LastPaymentTime == detallesSuscripcion.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == detallesSuscripcion.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == detallesSuscripcion.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == detallesSuscripcion.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == detallesSuscripcion.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == detallesSuscripcion.CyclesRemaining &&
                        existingSubscription.TotalCycles == detallesSuscripcion.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == detallesSuscripcion.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == detallesSuscripcion.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == detallesSuscripcion.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == detallesSuscripcion.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                       
                        _context.SubscriptionDetails.Update(detallesSuscripcion);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                 
                    _context.SubscriptionDetails.Add(detallesSuscripcion);
                    await _context.SaveChangesAsync();
                }

               
                return View(detallesSuscripcion);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al obtener los detalles de la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
       
        public async Task<ActionResult> TodasSuscripciones([FromQuery] Paginacion paginacion)
        {
            try
            {
               
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Consulta inicial para obtener los detalles de suscripción
                var queryable = from p in _context.SubscriptionDetails select p;

                // Llamada al método de extensión para obtener el total de páginas disponibles
                await HttpContext.TotalPaginas(queryable, paginacion.CantidadAMostrar);

                // Obtiene los usuarios con la paginación aplicada
                var usuarios = _policyExecutor.ExecutePolicy(() => queryable.Paginar(paginacion).ToList());

                // Obtiene el número total de páginas para la paginación
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();

                // Genera las páginas que se deben mostrar en la vista
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

               
                return View(usuarios);
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home");
            }
        }
        public async Task<IActionResult> ObtenerSuscripcionUsuario()
       {
          var usuarioActual = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
          int usuarioId;
          List<UserSubscription> suscripcionesUsuario = new List<UserSubscription>();
         if (usuarioActual != null)
          {
            if (int.TryParse(usuarioActual, out usuarioId))
             {
               // Consulta para obtener todas las suscripciones del usuario
               suscripcionesUsuario = await _context.UserSubscriptions
               .Include(x => x.User)
               .Where(x => x.UserId == usuarioId)
               .ToListAsync();
             }
          }

           return View(suscripcionesUsuario); 
        }

       

    }
}