# Gu�a de Uso del Patr�n Unit of Work

## Introducci�n

El patr�n **Unit of Work** (Unidad de Trabajo) es un patr�n de dise�o que centraliza la coordinaci�n de operaciones relacionadas con datos, como las interacciones con una base de datos o servicios externos. Su objetivo es garantizar que m�ltiples operaciones se realicen de manera consistente, facilitando la gesti�n de transacciones y mejorando la organizaci�n del c�digo.

## �Por Qu� Usar el Patr�n Unit of Work?

1. **Coordinaci�n de Operaciones**: Facilita la ejecuci�n de operaciones que involucran m�ltiples repositorios o servicios, asegurando que se completen de forma conjunta.
2. **Gesti�n de Transacciones**: En contextos de bases de datos, permite manejar transacciones para garantizar que todas las operaciones se confirmen o deshagan correctamente.
3. **Mantenibilidad y Extensibilidad**: Centraliza la l�gica de coordinaci�n, haciendo el c�digo m�s f�cil de mantener y ampliable para futuras funcionalidades, como agregar nuevos repositorios o notificaciones.

## Regla de Oro del Unit of Work

**El Unit of Work debe coordinar repositorios, no servicios, y los repositorios no deben depender de servicios que a su vez dependan de ellos.**

### �Qu� significa esto?
- **Coordinar repositorios, no servicios**: El Unit of Work debe gestionar operaciones de repositorios (que acceden directamente a la base de datos) en lugar de servicios de aplicaci�n (que contienen l�gica de negocio o interact�an con APIs externas). Esto evita que el Unit of Work se vuelva un contenedor de l�gica compleja y reduce el riesgo de ciclos de dependencias.
- **Evitar dependencias circulares**: Los repositorios no deben inyectar servicios (como `IPaypalService`) que dependan de ellos mismos, ya que esto crea ciclos de dependencias que causan errores en la inyecci�n de dependencias (por ejemplo, `InvalidOperationException`).
- **Por qu� es importante**: Seguir esta regla mantiene el c�digo limpio, evita errores dif�ciles de depurar y asegura que el Unit of Work cumpla su prop�sito de coordinar operaciones de datos sin mezclarse con l�gica de negocio.

### Ejemplo Pr�ctico de la Regla
Si un repositorio como `PaypalRepository` necesita datos de un servicio como `PaypalServices`, no debe inyectar `IPaypalService`. 
En lugar de eso, el Unit of Work o un controlador debe obtener los datos del servicio y pasarlos al repositorio como par�metros. 
Esto rompe posibles ciclos de dependencias y mantiene las responsabilidades separadas.

## Ejemplo de Implementaci�n

A continuaci�n, se muestra un ejemplo de c�mo implementar un Unit of Work que coordina operaciones de repositorios, siguiendo la regla de oro. En este caso, coordinamos la creaci�n de un producto en PayPal (a trav�s de un servicio) y el guardado de sus detalles en la base de datos (a trav�s de un repositorio), junto con el env�o de un correo electr�nico de notificaci�n.

### C�digo de Ejemplo

```csharp
using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace GestorInventario.Application.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        /* 
      * Esta clase act�a como un punto de coordinaci�n para los servicios relacionados, como PaypalServices.
      * Su prop�sito es facilitar la gesti�n y posible expansi�n futura, donde varios servicios pueden necesitar ser coordinados.
      * Aunque actualmente solo gestiona PaypalServices, se ha dise�ado para permitir la integraci�n de otros servicios en el futuro,
      * si se necesita coordinaci�n entre ellos. Este patr�n ayuda a mantener una estructura m�s organizada y extensible.
      */
        //Combina PaypalService y EmailService
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalServices> _logger;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMemoryCache _cache;
      private readonly IPaypalServiceRepository _paypalRepository;
        public UnitOfWork(IConfiguration configuration, ILogger<PaypalServices> logger, GestorInventarioContext context, 
            IEmailService email, IHttpContextAccessor contextAccessor, IMemoryCache m, IPaypalServiceRepository repo)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _emailService = email;
            _cache = m;
         _paypalRepository = repo;
            PaypalService = new PaypalServices(_configuration, _context, _logger, _cache, _paypalRepository);
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
                // Enviar el correo electr�nico
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

````