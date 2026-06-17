using GestorInventario.enums.Email;
using GestorInventario.Interfaces.Application.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MimeKit;
using MimeKit.Text;



namespace GestorInventario.Application.Services.Email_Services
{
    public class BaseEmail: IBaseEmail
    {
        public readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        public BaseEmail(IConfiguration configuration, IServiceProvider service, ICompositeViewEngine composite, ITempDataProvider tempdata)
        {
            _configuration = configuration;
            _serviceProvider = service;
            _viewEngine = composite;
            _tempDataProvider = tempdata;
        }
        public  async Task<bool> BuildEmail(string correo, string subject, EmailView view, object model) 
        {
            // Configurar y enviar el correo (esto sí le corresponde al servicio de email)
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration.GetSection("Email:UserName").Value));
            email.To.Add(MailboxAddress.Parse(correo));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = await RenderViewToStringAsync(view.ToViewName(), model)
            };

            using var smtp = new SmtpClient();
            var emailHost = Environment.GetEnvironmentVariable("Email__Host") ?? _configuration.GetSection("Email:Host").Value;
            var emailPortString = Environment.GetEnvironmentVariable("Email__Port") ?? _configuration.GetSection("Email:Port").Value;
            int emailPort = int.Parse(emailPortString ?? "587");

            await smtp.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);

            var emailUserName = Environment.GetEnvironmentVariable("Email__Username") ?? _configuration.GetSection("Email:UserName").Value;
            var emailPassWord = Environment.GetEnvironmentVariable("Email__Password") ?? _configuration.GetSection("Email:PassWord").Value;

            await smtp.AuthenticateAsync(emailUserName, emailPassWord);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
            return true;
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
