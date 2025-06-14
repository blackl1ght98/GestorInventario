using GestorInventario.Application.DTOs;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using MimeKit.Text;
using MimeKit;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension;

namespace GestorInventario.Application.Services
{
    public class EmailService:IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GestorInventarioContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly HashService _hashService;
        public EmailService(IConfiguration config, IHttpContextAccessor httpContextAccessor,
            GestorInventarioContext context, ITempDataProvider tempDataProvider,
            ICompositeViewEngine viewEngine, IServiceProvider serviceProvider, HashService hashService)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _tempDataProvider = tempDataProvider;
            _viewEngine = viewEngine;
            _serviceProvider = serviceProvider;
            _hashService = hashService;
        }
        //En este servicio esta la logica para enviar correo electronico
        public async Task SendEmailAsyncRegister(DTOEmail userDataRegister)
        {
           
            var usuarioDB = await _context.Usuarios.AsTracking().FirstOrDefaultAsync(x => x.Email == userDataRegister.ToEmail);
            //Genera un id aleatorio
            Guid miGuid = Guid.NewGuid();
            //El id generado aleatoriamente lo convierte a base 64 y esta tranformacion la transforma a un array de bytes
            string textoEnlace = Convert.ToBase64String(miGuid.ToByteArray());
            //Quita los caracteres especiales del id que se ha convertido en base 64
            textoEnlace = textoEnlace.Replace("=", "").Replace("+", "").Replace("/", "").Replace("?", "").Replace("&", "").Replace("!", "").Replace("¡", "");
            usuarioDB.EnlaceCambioPass = textoEnlace;         
            var model = new DTOEmail
            {
                
                //Cuando el usuario hace clic en el enlace que se le envia al correo electroni va al enspoint de confirmacion de correo electronico, en dicho endpoint los parametros llegan por ruta
                RecoveryLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/AdminController/ConfirmRegistration/{usuarioDB.Id}/{usuarioDB.EnlaceCambioPass}?redirect=true",
            };
            await _context.UpdateEntityAsync(usuarioDB);
            var ruta = await RenderViewToStringAsync("ViewsEmailService/ViewRegisterEmail", model);
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
            email.To.Add(MailboxAddress.Parse(userDataRegister.ToEmail));
            email.Subject = "Confirmar Email";
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = await RenderViewToStringAsync("ViewsEmailService/ViewRegisterEmail", model)
            };          
            using var smtp = new SmtpClient();
            var emailHost =  Environment.GetEnvironmentVariable("Email__Host") ?? _config.GetSection("Email:Host").Value;
            var emailPortString =  Environment.GetEnvironmentVariable("Email__Port")?? _config.GetSection("Email:Port").Value;
            int emailPort=int.Parse(emailPortString);
            await smtp.ConnectAsync(emailHost,emailPort,SecureSocketOptions.StartTls);
            var emailUserName = Environment.GetEnvironmentVariable("Email__Username")?? _config.GetSection("Email:UserName").Value;
            var emailPassWord = Environment.GetEnvironmentVariable("Email__Password")?? _config.GetSection("Email:PassWord").Value;
            await smtp.AuthenticateAsync(emailUserName, emailPassWord);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendEmailAsyncResetPassword(DTOEmail userDataResetPassword)
        {
            var usuarioDB = await _context.Usuarios.AsTracking().FirstOrDefaultAsync(x => x.Email == userDataResetPassword.ToEmail);
            // Generar una contraseña temporal
            var contrasenaTemporal = GenerarContrasenaTemporal();
            // Hashear la contraseña temporal y guardarla en la base de datos
            var resultadoHash = _hashService.Hash(contrasenaTemporal);
            usuarioDB.TemporaryPassword = resultadoHash.Hash;
            usuarioDB.Salt = resultadoHash.Salt;      
            await  _context.UpdateEntityAsync(usuarioDB);
            var fechaExpiracion = DateTime.Now.AddMinutes(5); 
            // Guardar la fecha de vencimiento en la base de datos
            usuarioDB.FechaEnlaceCambioPass = fechaExpiracion;
            usuarioDB.FechaExpiracionContrasenaTemporal= fechaExpiracion;     
            await _context.UpdateEntityAsync(usuarioDB);
            // Crear el modelo para la vista del correo electrónico
            var model = new DTOEmail
            {
                //Cuando el usuario hace clic en el enlace que se le envia al correo electronico es dirigido la endpoint de restaurar la contraseña(RestorePassword)
                RecoveryLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/AuthController/RestorePassword/{usuarioDB.Id}/{usuarioDB.EnlaceCambioPass}?redirect=true",
                TemporaryPassword = contrasenaTemporal
            };          
            var ruta = await RenderViewToStringAsync("ViewsEmailService/ViewResetPasswordEmail", model);
            // Crear el correo electrónico
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
            await smtp.AuthenticateAsync(_config.GetSection("Email:UserName").Value, _config.GetSection("Email:PassWord").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendEmailAsyncResetPasswordOlvidada(DTOEmail userDataResetPassword)
        {
            var usuarioDB = await _context.Usuarios.AsTracking().FirstOrDefaultAsync(x => x.Email == userDataResetPassword.ToEmail);
            // Generar una contraseña temporal
            var contrasenaTemporal = GenerarContrasenaTemporal();
            // Hashear la contraseña temporal y guardarla en la base de datos
            var resultadoHash = _hashService.Hash(contrasenaTemporal);
            usuarioDB.TemporaryPassword = resultadoHash.Hash;
            usuarioDB.Salt = resultadoHash.Salt;
            await _context.UpdateEntityAsync(usuarioDB);
            var fechaExpiracion = DateTime.Now.AddMinutes(5);
            // Guardar la fecha de vencimiento en la base de datos
            usuarioDB.FechaEnlaceCambioPass = fechaExpiracion;
            usuarioDB.FechaExpiracionContrasenaTemporal = fechaExpiracion;
            await _context.UpdateEntityAsync(usuarioDB);
            // Crear el modelo para la vista del correo electrónico
            var model = new DTOEmail
            {
                //Cuando el usuario hace clic en el enlace que se le envia al correo electronico es dirigido la endpoint de restaurar la contraseña(RestorePassword)
                RecoveryLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/AuthController/RestorePasswordOlvidada/{usuarioDB.Email}/{usuarioDB.EnlaceCambioPass}?redirect=true",
                TemporaryPassword = contrasenaTemporal
            };
            var ruta = await RenderViewToStringAsync("ViewsEmailService/ViewResetPasswordEmail", model);
            // Crear el correo electrónico
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
            await smtp.AuthenticateAsync(_config.GetSection("Email:UserName").Value, _config.GetSection("Email:PassWord").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        private string GenerarContrasenaTemporal()
        {
            var length = 12; // Aumenta la longitud de la contraseña
            var random = new Random();
            // Incluye caracteres especiales además de letras y números
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task SendEmailAsyncLowStock(DTOEmail correo, Producto producto)
        {
            // Crear el modelo para la vista del correo electrónico
            var model = new DTOEmail
            {
                NombreProducto=producto.NombreProducto,
                Cantidad=producto.Cantidad,
                
            };
            // Crear el correo electrónico
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config.GetSection("Email:UserName").Value));
            email.To.Add(MailboxAddress.Parse(correo.ToEmail));
            email.Subject = "Recuperar Contraseña";
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

        public async Task SendEmailCreateProduct(DTOEmail correo, string productName)
        {
            // Crear el modelo para la vista del correo electrónico
            var model = new DTOEmail
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
        public async Task SendEmailAsyncRembolso(DTOEmailRembolso correo)
        {
           
            var empleados = await _context.Usuarios
                .Where(u => u.IdRolNavigation.Nombre == "Empleado") 
                .Select(u => u.Email)
                .ToListAsync();

           

            
            var model = new DTOEmailRembolso
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
