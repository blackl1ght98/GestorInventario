using GestorInventario.Application.Classes;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Domain.Models.ViewModels.Paypal;
using GestorInventario.enums;
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
    public class SuscriptionProductController : Controller
    {
        private readonly GestorInventarioContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GenerarPaginas _generarPaginas;
        private readonly PolicyHandler _PolicyHandler;
        private readonly ILogger<SuscriptionProductController> _logger;
        public SuscriptionProductController(GestorInventarioContext context, IUnitOfWork unit, GenerarPaginas generar, PolicyHandler policy, ILogger<SuscriptionProductController> logger)
        {
            _context = context;
            _unitOfWork = unit;
            _generarPaginas = generar;
            _PolicyHandler = policy;
            _logger = logger;
        }

      //Metodo que muestra al usuario a los productos que se ha suscrito
        public async Task<IActionResult> MostrarProductos(int pagina = 1)
        {
            // Establece el número de productos por página.
            int pageSize = 10;

            // Llama al método GetProductsAsync para obtener los productos y verificar si hay una página siguiente.
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

            // Construye el objeto Paginacion con los valores adecuados.
            var paginacion = new Paginacion
            {
                TotalPaginas = hasNextPage ? pagina + 1 : pagina,
                PaginaActual = pagina,
                Radio = 3 // Ajusta el radio según tus necesidades.
            };

            // Genera las páginas usando el objeto Paginacion.
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

            // Retorna la vista con el modelo que contiene los productos y la información de paginación.
            return View(model);
        }
        //Metodo que muestra los planes disponibles al usuario
        public async Task<IActionResult> MostrarPlanes(int pagina = 1)
        {
            int pageSize = 10;
            var (planesResponse, hasNextPage) = await _unitOfWork.PaypalService.GetSubscriptionPlansAsync(pagina, pageSize);
            dynamic planesJson = JsonConvert.DeserializeObject(planesResponse);

            var planes = new List<PlanesViewModel>();
            foreach (var plan in planesJson.plans)
            {
                planes.Add(new PlanesViewModel
                {
                    id = plan.id,
                    productId = plan.product_id,
                    name = plan.name,
                    description = plan.description,
                    status = plan.status,
                    usage_type = plan.usage_type,
                    createTime = plan.create_time
                });
            }

            // Construye el objeto Paginacion con los valores adecuados
            var paginacion = new Paginacion
            {
                TotalPaginas = hasNextPage ? pagina + 1 : pagina,
                PaginaActual = pagina,
                Radio = 3 // Ajusta el radio según tus necesidades
            };

            // Genera las páginas usando el objeto Paginacion
            var paginacionMetodo = new PaginacionMetodo();
            var paginas = paginacionMetodo.GenerarListaPaginas(paginacion);

            var model = new PlanesPaginadosViewModel
            {
                Planes = planes,
                Paginas = paginas,
                TienePaginaSiguiente = hasNextPage,
                TienePaginaAnterior = pagina > 1
            };

            return View(model);
        }

        // Método para crear la lista de categorías a partir de la enumeración
        private List<string> GetCategoriesFromEnum()
        {
            return Enum.GetNames(typeof(Category)).ToList();
        }
        //Metodo que obtiene los datos necesarios antes de crear el producto al que se suscribira
        public IActionResult CrearProducto()
        {
            // Crear una instancia del modelo
            var model = new ProductViewModelPaypal();

            // Obtener las categorías desde la enumeración y asignarlas al modelo
            model.Categories = GetCategoriesFromEnum();

            return View(model);
        }

      //metodo que crea el producto al que se suscribira
        [HttpPost]
        public async Task<IActionResult> CrearProducto(ProductViewModelPaypal model)
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

                    // Crear el plan de suscripción
                    string planResponse = await _unitOfWork.PaypalService.CreateSubscriptionPlanAsync(
                        productId,
                        model.PlanName,
                        model.PlanDescription,
                        model.Amount,
                        "EUR",
                        model.HasTrialPeriod ? model.TrialPeriodDays : 0, // Pasamos los días de prueba
                        model.HasTrialPeriod ? model.TrialAmount : 0.00m   // Pasamos el costo del periodo de prueba
                    );

                    // Verificar si hubo un error en la creación del plan de suscripción
                    if (planResponse.Contains("\"error\""))
                    {
                        return StatusCode(500, $"Error al crear el plan de suscripción: {planResponse}");
                    }

                    return RedirectToAction(nameof(MostrarProductos));
                }
                catch (Exception ex)
                {
                    // Retornar un mensaje de error con detalles para diagnosticar
                    return StatusCode(500, $"Error inesperado: {ex.Message}");
                }
            }

            // Si el modelo no es válido, volvemos a asignar las categorías al modelo
            model.Categories = GetCategoriesFromEnum();
            return View(model);
        }

        //Metodo para desactivar el plan
        [HttpPost]
        public async Task<IActionResult> DesactivarPlan(string productId, string planId)
        {
            try
            {
                // Llama al método para eliminar el producto y su plan
                var deleteResponse = await _unitOfWork.PaypalService.DesactivarPlan(productId, planId);

                // Redirige o muestra un mensaje de éxito
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
                // Llama al método para eliminar el producto y su plan
                var deleteResponse = await _unitOfWork.PaypalService.MarcarDesactivadoProducto(id);

                // Redirige o muestra un mensaje de éxito
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
                        // Crear un producto usando los datos del formulario
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
            string returnUrl = Url.Action("ConfirmarSuscripcion", "SuscriptionProduct", null, Request.Scheme);
            string cancelUrl = Url.Action("CancelarSuscripcion", "SuscriptionProduct", null, Request.Scheme);

           

            try
            {
                // Llama al método Subscribirse para obtener la URL de aprobación
                string approvalUrl = await _unitOfWork.PaypalService.Subscribirse(plan_id, returnUrl, cancelUrl, planName);

                // Redirige al usuario a la URL de aprobación de PayPal
                return Redirect(approvalUrl);
            }
            catch (Exception ex)
            {
                // Manejar la excepción y mostrar un mensaje de error
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo para confirmar la suscripcion
        public async Task<IActionResult> ConfirmarSuscripcion(string subscription_id, string token, string ba_token)
        {
            try
            {
                if (string.IsNullOrEmpty(subscription_id))
                {
                    // Manejar el caso en el que no se proporciona un ID de suscripción válido
                    TempData["ErrorMessage"] = "No se pudo confirmar la suscripción. Falta el ID de la suscripción.";
                    return RedirectToAction("Error", "Home");
                }

                // Aquí puedes realizar cualquier acción necesaria con la suscripción, como guardarla en la base de datos
                // o actualizar el estado del usuario a "suscrito".

                TempData["SuccessMessage"] = "¡Suscripción confirmada con éxito!";
                return RedirectToAction("DetallesSuscripcion", new { id = subscription_id }); // Redirigir a una página de detalles de la suscripción
            }
            catch (Exception ex)
            {
                // Manejar cualquier error que ocurra durante el procesamiento
                TempData["ErrorMessage"] = $"Error al confirmar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }

        //Metodo que cancela la suscripcion
        public async Task<IActionResult> CancelarSuscripcion(string subscription_id)
        {
          
        
            try
            {
                //ver si una suscripcion cancelada se puede reactivar
                string result = await _unitOfWork.PaypalService.CancelarSuscripcion(subscription_id,"No satisfecho");
                var actualizar = await _context.SubscriptionDetails.FirstOrDefaultAsync(x=>x.SubscriptionId==subscription_id);
                if (actualizar != null)
                {
                    if (actualizar.Status == "ACTIVE" || actualizar.Status == "SUSPEND")
                    {
                        actualizar.Status = "CANCELLED";
                        await _context.UpdateEntityAsync(actualizar);
                    }
                }
                // Redirige al usuario a la URL de aprobación de PayPal
                return RedirectToAction(nameof(TodasSuscripciones)); 
            }
            catch (Exception ex)
            {
                // Manejar la excepción y mostrar un mensaje de error
                TempData["ErrorMessage"] = $"Error al iniciar la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }
        //Metodo para obtener los detalles del plan
        private async Task DetallesPlan(string id)
        {
            /**
             * IMPORTANTE:
             * NO CAMBIAR ESTRUCTURA ES COMO TIENE QUE ESTAR PARA QUE SE MUESTRE BIEN LA INFORMACION
             * */
            try
            {
                var existingPlan = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == id);
                if (existingPlan != null)
                {
                    return; 
                }
                var planRequest = await _unitOfWork.PaypalService.ObtenerDetallesPlan(id);
                var planDetails = new PlanDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    PaypalPlanId = planRequest.id,
                    ProductId = planRequest.product_id,
                    Name = planRequest.name,
                    Description = planRequest.description,
                    Status = planRequest.status,
                    AutoBillOutstanding = planRequest.payment_preferences.auto_bill_outstanding,
                    SetupFee = planRequest.payment_preferences.setup_fee?.value != null ?
                decimal.Parse(planRequest.payment_preferences.setup_fee.value.ToString(), CultureInfo.InvariantCulture) : 0,
                    SetupFeeFailureAction = planRequest.payment_preferences.setup_fee_failure_action,
                    PaymentFailureThreshold = planRequest.payment_preferences.payment_failure_threshold,
                    TaxPercentage = planRequest.taxes?.percentage != null ?
                     decimal.Parse(planRequest.taxes.percentage.ToString(), CultureInfo.InvariantCulture) : 0,
                    TaxInclusive = planRequest.taxes?.inclusive ?? false
                };


               
                // Verificar si existe un ciclo de facturación de prueba si esta condicion se cumple es que existe dias de prueba.
                if (planRequest.billing_cycles.Count > 1)
                {
                    planDetails.TrialIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                    planDetails.TrialIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                    planDetails.TrialTotalCycles = planRequest.billing_cycles[0].total_cycles;

                    // Convertir TrialFixedPrice a decimal si no es nulo
                    planDetails.TrialFixedPrice = planRequest.billing_cycles[0].pricing_scheme.fixed_price?.value != null ?
                                                  decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value.ToString(), CultureInfo.InvariantCulture) : 0;

                    // Información del ciclo regular
                    planDetails.RegularIntervalUnit = planRequest.billing_cycles[1].frequency.interval_unit;
                    planDetails.RegularIntervalCount = planRequest.billing_cycles[1].frequency.interval_count;
                    planDetails.RegularTotalCycles = planRequest.billing_cycles[1].total_cycles;

                    // Convertir RegularFixedPrice a decimal si no es nulo
                    planDetails.RegularFixedPrice = planRequest.billing_cycles[1].pricing_scheme.fixed_price?.value != null ?
                                                    decimal.Parse(planRequest.billing_cycles[1].pricing_scheme.fixed_price.value.ToString(), CultureInfo.InvariantCulture) : 0;
                }

                else if (planRequest.billing_cycles.Count == 1)
                {
                    // Solo hay ciclo regular
                    planDetails.RegularIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                    planDetails.RegularIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                    planDetails.RegularTotalCycles = planRequest.billing_cycles[0].total_cycles;
                    planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
                }

                _context.PlanDetails.Add(planDetails);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar los detalles del plan en la base de datos: {ex.Message}", ex);
               
            }
        }
        public async Task<IActionResult> DetallesSuscripcion(string id)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                // Obtener detalles de la suscripción desde PayPal
                var subscriptionDetails = await _unitOfWork.PaypalService.ObtenerDetallesSuscripcion(id);

                // Convertir plan_id a string para evitar problemas con árboles de expresión
                string planId = (string)subscriptionDetails.plan_id;

                // Obtener los detalles del plan desde la base de datos usando el PlanId
                var plan = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);

                if (plan == null)
                {
                    await DetallesPlan(planId);
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
                var existingSubscription = await _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == detallesSuscripcion.SubscriptionId);

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
                        // Actualizar la suscripción existente
                        _context.SubscriptionDetails.Update(detallesSuscripcion);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Guardar los detalles de la nueva suscripción en la base de datos
                    _context.SubscriptionDetails.Add(detallesSuscripcion);
                    await _context.SaveChangesAsync();
                }

                // Pasar los detalles de la suscripción a la vista
                return View(detallesSuscripcion);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al obtener los detalles de la suscripción: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }


        //Metodo que muestra todas la suscricpciones
        //public async Task<ActionResult> TodasSuscripciones( [FromQuery] Paginacion paginacion)
        //{
        //    try
        //    {

        //        if (!User.Identity.IsAuthenticated)
        //        {
        //            return RedirectToAction("Login", "Auth");
        //        }
        //        var queryable = from p in _context.SubscriptionDetails select p;


        //        //Accedemos al metodo de extension creado pasandole la fuente de informacion(queryable) y las paginas a mostrar
        //        await HttpContext.TotalPaginas(queryable, paginacion.CantidadAMostrar);
        //        var usuarios = ExecutePolicy(() => queryable.Paginar(paginacion).ToList());
        //        //Obtiene los datos de la cabecera que hace esta peticion
        //        var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();
        //        //Crea las paginas que el usuario ve.
        //        ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

        //        return View(usuarios);
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
        //        _logger.LogError(ex, "Error al obtener los datos del usuario");
        //        return RedirectToAction("Error", "Home");

        //    }
        //}
        public async Task<ActionResult> TodasSuscripciones([FromQuery] Paginacion paginacion)
        {
            try
            {
                // Verifica si el usuario está autenticado
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Consulta inicial para obtener los detalles de suscripción
                var queryable = from p in _context.SubscriptionDetails select p;

                // Llamada al método de extensión para obtener el total de páginas disponibles
                await HttpContext.TotalPaginas(queryable, paginacion.CantidadAMostrar);

                // Obtiene los usuarios con la paginación aplicada
                var usuarios = ExecutePolicy(() => queryable.Paginar(paginacion).ToList());

                // Obtiene el número total de páginas para la paginación
                var totalPaginas = HttpContext.Response.Headers["totalPaginas"].ToString();

                // Genera las páginas que se deben mostrar en la vista
                ViewData["Paginas"] = _generarPaginas.GenerarListaPaginas(int.Parse(totalPaginas), paginacion.Pagina);

                // Verifica la existencia del usuario en la base de datos
                var existeUsuario = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;

                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    // Itera sobre las suscripciones obtenidas para verificar o crear relaciones de suscripción
                    foreach (var detallesSuscripcion in usuarios)
                    {
                        var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.NombreCompleto == detallesSuscripcion.SubscriberName);

                        if (usuario != null)
                        {
                            // Verifica si ya existe una relación de suscripción para este usuario
                            var existeRelacion = await _context.UserSubscriptions
                                .FirstOrDefaultAsync(us => us.UserId == usuarioId && us.SubscriptionId == detallesSuscripcion.SubscriptionId);

                            if (existeRelacion == null)
                            {
                                // Si no existe la relación, la crea
                                var userSubscription = new UserSubscription
                                {
                                    UserId = usuarioId,  // Asocia el ID del usuario
                                    SubscriptionId = detallesSuscripcion.SubscriptionId,
                                    NombreSusbcriptor = detallesSuscripcion.SubscriberName
                                };

                                // Guarda la relación en la tabla intermedia
                                _context.UserSubscriptions.Add(userSubscription);
                                await _context.SaveChangesAsync();
                            }
                        }
                       
                    }
                }

                // Devuelve la vista con los usuarios paginados
                return View(usuarios);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder. Inténtelo de nuevo más tarde.";
                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home");
            }
        }


        //Metodo para obtener los detalles del plan
        public async Task<IActionResult> GetPlanDetailsIfNotExists(string subscriptionId)
        {
            // Primero, obtener la suscripción desde la base de datos
            var subscription = await _context.SubscriptionDetails
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

            if (subscription == null)
            {
                return Json(new { trialDays = "Error: Suscripción no encontrada" });
            }

            // Ahora, obtener los detalles del plan desde la base de datos
            var plan = await _context.PlanDetails
                .FirstOrDefaultAsync(p => p.PaypalPlanId == subscription.PlanId);

            // Si no existe el plan, obtenerlo desde PayPal
            if (plan == null)
            {
                // Llama al método que obtiene y guarda los detalles del plan desde PayPal
                await DetallesPlan(subscription.PlanId);

                // Vuelve a cargar el plan desde la base de datos
                plan = await _context.PlanDetails
                    .FirstOrDefaultAsync(p => p.PaypalPlanId == subscription.PlanId);
            }

            // Devolver los días de prueba del plan
            return Json(new { trialDays = plan.TrialIntervalCount });
        }


      



   



        private async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<T>();
            return await policy.ExecuteAsync(operation);
        }
        private T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }



    }

}





