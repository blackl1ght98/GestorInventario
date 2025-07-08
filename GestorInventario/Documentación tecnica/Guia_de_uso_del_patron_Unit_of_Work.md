# Guía de Uso del Patrón Unit of Work

## Introducción

El patrón **Unit of Work** (Unidad de Trabajo) es un patrón de diseño que centraliza la coordinación de operaciones relacionadas con datos, como las interacciones con una base de datos o servicios externos. Su objetivo es garantizar que múltiples operaciones se realicen de manera consistente, facilitando la gestión de transacciones y mejorando la organización del código.

## ¿Por Qué Usar el Patrón Unit of Work?

1. **Coordinación de Operaciones**: Facilita la ejecución de operaciones que involucran múltiples repositorios o servicios, asegurando que se completen de forma conjunta.
2. **Gestión de Transacciones**: En contextos de bases de datos, permite manejar transacciones para garantizar que todas las operaciones se confirmen o deshagan correctamente.
3. **Mantenibilidad y Extensibilidad**: Centraliza la lógica de coordinación, haciendo el código más fácil de mantener y ampliable para futuras funcionalidades, como agregar nuevos repositorios o notificaciones.

## Regla de Oro del Unit of Work

**El Unit of Work debe coordinar repositorios, no servicios, y los repositorios no deben depender de servicios que a su vez dependan de ellos.**

### ¿Qué significa esto?
- **Coordinar repositorios, no servicios**: El Unit of Work debe gestionar operaciones de repositorios (que acceden directamente a la base de datos) en lugar de servicios de aplicación (que contienen lógica de negocio o interactúan con APIs externas). Esto evita que el Unit of Work se vuelva un contenedor de lógica compleja y reduce el riesgo de ciclos de dependencias.
- **Evitar dependencias circulares**: Los repositorios no deben inyectar servicios (como `IPaypalService`) que dependan de ellos mismos, ya que esto crea ciclos de dependencias que causan errores en la inyección de dependencias (por ejemplo, `InvalidOperationException`).
- **Por qué es importante**: Seguir esta regla mantiene el código limpio, evita errores difíciles de depurar y asegura que el Unit of Work cumpla su propósito de coordinar operaciones de datos sin mezclarse con lógica de negocio.

### Ejemplo Práctico de la Regla
Si un repositorio como `PaypalRepository` necesita datos de un servicio como `PaypalServices`, no debe inyectar `IPaypalService`. 
En lugar de eso, el Unit of Work o un controlador debe obtener los datos del servicio y pasarlos al repositorio como parámetros. 
Esto rompe posibles ciclos de dependencias y mantiene las responsabilidades separadas.

## Ejemplo de Implementación

A continuación, se muestra un ejemplo de cómo implementar un Unit of Work que coordina operaciones de repositorios, siguiendo la regla de oro. En este caso, coordinamos la creación de un producto en PayPal (a través de un servicio) y el guardado de sus detalles en la base de datos (a través de un repositorio), junto con el envío de un correo electrónico de notificación.

### Código de Ejemplo

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
      * Esta clase actúa como un punto de coordinación para los servicios relacionados, como PaypalServices.
      * Su propósito es facilitar la gestión y posible expansión futura, donde varios servicios pueden necesitar ser coordinados.
      * Aunque actualmente solo gestiona PaypalServices, se ha diseñado para permitir la integración de otros servicios en el futuro,
      * si se necesita coordinación entre ellos. Este patrón ayuda a mantener una estructura más organizada y extensible.
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

````