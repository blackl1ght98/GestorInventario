using GestorInventario.Domain.enums.Email;
using GestorInventario.Interfaces.Application.Services;
using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using RazorLight;




namespace GestorInventario.Notifications.EmailServices
{
    public class BaseEmail: IBaseEmail
    {
        private readonly IConfiguration _configuration;
        private readonly RazorLightEngine _razorEngine;
        private readonly string _templatesRoot;

        public BaseEmail(IConfiguration configuration)
        {
            _configuration = configuration;

            // Apunta a GestorInventario.Notifications/ViewsEmailService/ dentro del output
            _templatesRoot = Path.Combine(AppContext.BaseDirectory, "ViewsEmailService");

            _razorEngine = new RazorLightEngineBuilder()
                .UseFileSystemProject(_templatesRoot)
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task<bool> BuildEmail(string correo, string subject, EmailView view, object model)
        {
            var email = new MimeMessage();
            var username = _configuration["Email:UserName"] ?? Environment.GetEnvironmentVariable("EMAIL_USERNAME");
            var password = _configuration["Email:PassWord"] ?? Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
            email.From.Add(MailboxAddress.Parse(username));
            email.To.Add(MailboxAddress.Parse(correo));
            email.Subject = subject;

            // view.ToViewName() devuelve "ViewsEmailService/ViewOtpCode" (sin extensión).
            // Le añadimos .cshtml y la ruta absoluta para que RazorLight la localice.
            var templatePath = Path.Combine(AppContext.BaseDirectory, view.ToViewName() + ".cshtml");
            var html = await _razorEngine.CompileRenderAsync(templatePath, model);

            email.Body = new TextPart(TextFormat.Html) { Text = html };

            using var smtp = new SmtpClient();
            var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST") ?? _configuration["Email:Host"];
            var emailPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT") ?? _configuration["Email:Port"] ?? "587");

            await smtp.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return true;
        }
    }

}

