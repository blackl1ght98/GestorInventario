using GestorInventario.Application.Interfaces.EmailServices;
using GestorInventario.Domain.enums.Email;
using GestorInventario.Infrastructure.Configuration;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using RazorLight;

namespace GestorInventario.Infrastructure.Notifications.Email.EmailServices
{
    public class BaseEmail: IBaseEmail
    {
       
        private readonly RazorLightEngine _razorEngine;
        private readonly string _templatesRoot;
        private EnvironmentConfig _environmentConfig;

        public BaseEmail(IConfiguration configuration)
        {
           

            // Apunta a GestorInventario.Notifications/ViewsEmailService/ dentro del output
            _templatesRoot = Path.Combine(AppContext.BaseDirectory, "Notifications", "Email", "ViewsEmailService");
          _environmentConfig = new EnvironmentConfig(configuration);
            _razorEngine = new RazorLightEngineBuilder()
                .UseFileSystemProject(_templatesRoot)
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task<bool> BuildEmail(string correo, string subject, EmailView view, object model)
        {
            var email = new MimeMessage();
            var username = _environmentConfig.EmailUser;
            var password = _environmentConfig.EmailPassword;
            email.From.Add(MailboxAddress.Parse(username));
            email.To.Add(MailboxAddress.Parse(correo));
            email.Subject = subject;

            try
            {
                // view.ToViewName() devuelve "ViewsEmailService/ViewOtpCode"
                // Como ya estamos dentro de la carpeta ViewsEmailService en el root,
                // necesitamos extraer solo el nombre del archivo "ViewOtpCode"
                var fileName = Path.GetFileName(view.ToViewName());
                var templateKey = fileName + ".cshtml";

              

                // Usamos la clave relativa al root configurado
                var html = await _razorEngine.CompileRenderAsync(templateKey, model);
                email.Body = new TextPart(TextFormat.Html) { Text = html };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR RazorLight: {ex.Message}");
                throw;
            }

            using var smtp = new SmtpClient();
            var emailHost = _environmentConfig.EmailHost;
            var emailPort = int.Parse(_environmentConfig.EmailPort);

            await smtp.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return true;
        }
    }

}

