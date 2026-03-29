using MailKit.Security;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using MimeKit.Text;
using MimeKit;
using MailKit.Net.Smtp;
using GestorInventario.Interfaces.Application;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.email;
using GestorInventario.ViewModels.order;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Application.DTOs;

namespace GestorInventario.Application.Services
{
    public class EmailService:IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;     
        private readonly IServiceProvider _serviceProvider;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<EmailService> _logger;
      
        public EmailService(IConfiguration config, IHttpContextAccessor httpContextAccessor, IUserRepository user,
            ITempDataProvider tempDataProvider, ILogger<EmailService> logger,
            ICompositeViewEngine viewEngine, IServiceProvider serviceProvider)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
           
            _tempDataProvider = tempDataProvider;
            _viewEngine = viewEngine;
            _serviceProvider = serviceProvider;
            
            _logger = logger;
            _userRepository = user;
           
        }

        public async Task<OperationResult<string>> SendEmailAsyncRegister(EmailDto userDataRegister, int usuarioId)
        {
            try
            {
                

                // Generar token
                string textoEnlace = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace("=", "").Replace("+", "").Replace("/", "")
                    .Replace("?", "").Replace("&", "").Replace("!", "").Replace("¡", "");

                // ← Llamada al repositorio (ya no usamos _context directamente)
                var resultadoToken = await _userRepository.ActualizarEmailVerificationTokenAsync(usuarioId, textoEnlace);

                if (!resultadoToken.Success)
                {
                    return OperationResult<string>.Fail("Error al actualizar el token de verificación");
                }

                // Construir el enlace de recuperación
                var model = new RegisterEmailViewmodel
                {
                    RecoveryLink = $"{_httpContextAccessor?.HttpContext?.Request.Scheme}://{_httpContextAccessor?.HttpContext?.Request.Host}/admin/confirm-registration/{usuarioId}/{textoEnlace}?redirect=true",
                };

                // Configurar y enviar el correo (esto sí le corresponde al servicio de email)
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
                email.To.Add(MailboxAddress.Parse(userDataRegister.ToEmail));
                email.Subject = "Confirmar Email";
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = await RenderViewToStringAsync("ViewsEmailService/ViewRegisterEmail", model)
                };

                using var smtp = new SmtpClient();
                var emailHost = Environment.GetEnvironmentVariable("Email__Host") ?? _config.GetSection("Email:Host").Value;
                var emailPortString = Environment.GetEnvironmentVariable("Email__Port") ?? _config.GetSection("Email:Port").Value;
                int emailPort = int.Parse(emailPortString ?? "587");

                await smtp.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);

                var emailUserName = Environment.GetEnvironmentVariable("Email__Username") ?? _config.GetSection("Email:UserName").Value;
                var emailPassWord = Environment.GetEnvironmentVariable("Email__Password") ?? _config.GetSection("Email:PassWord").Value;

                await smtp.AuthenticateAsync(emailUserName, emailPassWord);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return OperationResult<string>.Ok("Operacion realizada con exito");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail($"Ocurrio un error al enviar el email: {ex.Message}");
            }
        }

        public async Task<OperationResult<string>> SendEmailAsyncResetPassword(EmailDto userDataResetPassword, int usuarioId)
        {
            try
            {
                // 1. Llamada al repositorio para toda la lógica de BD
                var resultado = await _userRepository.GenerarYGuardarPasswordTemporalAsync(userDataResetPassword.ToEmail);

                if (!resultado.Success)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }

                var (contrasenaTemporal, token) = resultado.Data;   // desestructuramos

                // 2. Solo preparar el modelo y enviar el correo
                var model = new ResetPasswordEmailViewmodel
                {
                    RecoveryLink = $"{_httpContextAccessor?.HttpContext?.Request.Scheme}://{_httpContextAccessor?.HttpContext?.Request.Host}/auth/restore-password/{usuarioId}/{token}?redirect=true",
                    TemporaryPassword = contrasenaTemporal
                };

                // Configurar y enviar el correo
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
                email.To.Add(MailboxAddress.Parse(userDataResetPassword.ToEmail));
                email.Subject = "Recuperar Contraseña";
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = await RenderViewToStringAsync("ViewsEmailService/ViewResetPasswordEmail", model)
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _config.GetSection("Email:Host").Value,
                    Convert.ToInt32(_config.GetSection("Email:Port").Value),
                    SecureSocketOptions.StartTls
                );

                await smtp.AuthenticateAsync(
                    _config.GetSection("Email:UserName").Value, 
                    _config.GetSection("Email:PassWord").Value
                );

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return OperationResult<string>.Ok("Envio de correo exitoso", userDataResetPassword.ToEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de restablecimiento de contraseña");
                return OperationResult<string>.Fail($"Ocurrió un error al enviar el email: {ex.Message}");
            }
        }
      
        public async Task SendEmailAsyncLowStock(EmailDto correo, LowStockEmailData  producto)
        {

            // Crear el modelo para la vista del correo electrónico
            var model = new LowStockViewmodel
            {
                NombreProducto=producto.NombreProducto,
                Cantidad=producto.Cantidad,
                
            };
            // Crear el correo electrónico
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
            email.To.Add(MailboxAddress.Parse(correo.ToEmail));
            email.Subject = "Alerta: Bajo stock de producto";
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = await RenderViewToStringAsync("ViewsEmailService/ViewLowStock", model)
            };          
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config.GetSection("Email:Host").Value,
                Convert.ToInt32(_config.GetSection("Email:Port").Value),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(_config.GetSection("Email:UserName").Value, _config.GetSection("Email:PassWord").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendEmailAsyncFactura(EmailDto correo, string id)
        {
            // Crear el modelo para la vista del correo electrónico
            var model = new FacturaViewmodel
            {
                IdPago = id,
                EnlaceDescarga = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/Pedidos/DownloadInvoice?id={id}",

            };
            // Crear el correo electrónico
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
            email.To.Add(MailboxAddress.Parse(correo.ToEmail));
            email.Subject = "Descargar factura";
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = await RenderViewToStringAsync("ViewsEmailService/ViewDownloadFactura", model)
            };
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config.GetSection("Email:Host").Value,
                Convert.ToInt32(_config.GetSection("Email:Port").Value),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(_config.GetSection("Email:UserName").Value, _config.GetSection("Email:PassWord").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendEmailCreateProduct(EmailDto correo, string productName)
        {
            // Crear el modelo para la vista del correo electrónico
            var model = new CreateProductEmailViewmodel
            {
                NombreProducto = productName
               
            };
            // Crear el correo electrónico
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
            email.To.Add(MailboxAddress.Parse(correo.ToEmail));
            email.Subject = "Producto creado paypal";
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = await RenderViewToStringAsync("ViewsEmailService/ViewCreateProductPaypal", model)
            };

            // Enviar el correo electrónico
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config.GetSection("Email:Host").Value,
                Convert.ToInt32(_config.GetSection("Email:Port").Value),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(_config.GetSection("Email:UserName").Value, _config.GetSection("Email:PassWord").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public async Task EnviarEmailSolicitudRembolso(EmailRembolsoDto correo)
        {

            var empleados = await _userRepository.ObtenerEmailsEmpleadosAsync();
            if (empleados != null) {
                var model = new EmailRembolsoViewmodel
                {
                    NumeroPedido = correo.NumeroPedido,
                    NombreCliente = correo.NombreCliente,
                    EmailCliente = correo.EmailCliente,
                    FechaRembolso = correo.FechaRembolso,
                    CantidadADevolver = correo.CantidadADevolver,
                    MotivoRembolso = correo.MotivoRembolso,
                    Productos = correo.Productos
                };
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
                // Agregar cada empleado como destinatario
                foreach (var empleadoEmail in empleados)
                {
                    email.To.Add(MailboxAddress.Parse(empleadoEmail));
                }

                email.Subject = "Notificación de Reembolso Realizado";
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = await RenderViewToStringAsync("ViewsEmailService/ViewRembolso", model)
                };
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _config.GetSection("Email:Host").Value,
                    Convert.ToInt32(_config.GetSection("Email:Port").Value),
                    SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(_config.GetSection("Email:UserName").Value, _config.GetSection("Email:PassWord").Value);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

            }
           
        }

        public async Task EnviarNotificacionReembolsoAsync(EmailReembolsoAprobadoDto correo)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
                email.To.Add(MailboxAddress.Parse(correo.EmailCliente)); // Enviar solo al usuario
                email.Subject = $"Tu reembolso para el pedido #{correo.NumeroPedido} ha sido aprobado";
                var viewmodel = new RembolsoAprobadoViewmodel
                {
                    NumeroPedido = correo.NumeroPedido,
                    NombreCliente = correo.NombreCliente,
                    EmailCliente = correo.EmailCliente,
                    FechaRembolso = correo.FechaRembolso,
                    CantidadADevolver = correo.CantidadADevolver,
                    MotivoRembolso = correo.MotivoRembolso,
                    Productos = correo.Productos,
                };

                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = await RenderViewToStringAsync("ViewsEmailService/ViewRembolsoAprobado", viewmodel)
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _config.GetSection("Email:Host").Value,
                    Convert.ToInt32(_config.GetSection("Email:Port").Value),
                    SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(
                    _config.GetSection("Email:UserName").Value,
                    _config.GetSection("Email:PassWord").Value
                );
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Se ha producido un error al enviar la notificacion");
                throw;
            }
        }

        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(actionContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return sw.ToString();
            }


        }
    }
}
