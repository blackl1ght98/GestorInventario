using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;

namespace GestorInventario.Application.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        /* 
      * Esta clase actúa como un punto de coordinación para los servicios relacionados, como PaypalServices.
      * Su propósito es facilitar la gestión y posible expansión futura, donde varios servicios pueden necesitar ser coordinados.
      * Aunque actualmente solo gestiona PaypalServices, se ha diseñado para permitir la integración de otros servicios en el futuro,
      * si se necesita coordinación entre ellos. Este patrón ayuda a mantener una estructura más organizada y extensible.
      */

        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalServices> _logger;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _contextAccessor;
        public UnitOfWork(IConfiguration configuration, ILogger<PaypalServices> logger, GestorInventarioContext context, IEmailService email, IHttpContextAccessor contextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _emailService = email;
            PaypalService = new PaypalServices(_configuration, _context, _logger);
            _contextAccessor = contextAccessor;
        }
        public IPaypalService PaypalService { get; private set; }
        public async Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory)
        {
            // Crear el producto
            var response = await PaypalService.CreateProductAsync(productName, productDescription, productType, productCategory);

            if (response.IsSuccessStatusCode)
            {
                var email = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                // Enviar el correo electrónico
                var emailDto = new DTOEmail
                {
                    ToEmail = email,
                    NombreProducto = productName
                };

                await _emailService.SendEmailCreateProduct(emailDto, productName);

                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                throw new Exception($"Error al crear el producto: {response.StatusCode} - {response.Content.ReadAsStringAsync().Result}");
            }
        }
    }
}
