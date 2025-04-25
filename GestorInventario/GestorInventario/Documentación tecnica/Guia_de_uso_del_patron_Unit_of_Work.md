# Guía de Uso del Patrón Unit of Work

## Introducción

El patrón **Unit of Work** (Unidad de Trabajo) es un patrón de diseño que se utiliza principalmente en aplicaciones que interactúan con bases de datos, pero también 
puede aplicarse a otros contextos donde se necesita coordinar la interacción entre varios servicios. El objetivo principal del patrón es centralizar la lógica de las 
operaciones que deben ejecutarse de manera conjunta, permitiendo una mejor organización del código y facilitando la gestión de transacciones.

## ¿Por Qué Usar el Patrón Unit of Work?

1. **Coordinación de Múltiples Servicios**: Cuando tu aplicación necesita realizar operaciones que involucran varios servicios (por ejemplo, crear un producto y 
 enviar un correo electrónico), el patrón Unit of Work ayuda a coordinar estas operaciones en un solo punto.

2. **Gestión de Transacciones**: Aunque en este caso no estamos interactuando directamente con una base de datos, el patrón se inspira en la gestión de transacciones. 
 La idea es asegurar que todas las operaciones se completen correctamente antes de confirmar el "commit" de la transacción. Si algo falla, se puede "deshacer" todo lo 
 que se hizo hasta ese punto.

3. **Mantenibilidad y Extensibilidad**: Centralizar la lógica de coordinación de servicios en un solo lugar facilita la mantenibilidad del código. Además, si en el futuro 
 se necesita añadir más servicios (como notificaciones por SMS, registros en logs, etc.), se pueden integrar fácilmente en el Unit of Work.

## Ejemplo de Implementación

A continuación, se presenta un ejemplo de cómo implementar un Unit of Work para coordinar la creación de un producto en PayPal y el envío de un correo electrónico de notificación.

### Código de Ejemplo

````sh
public class UnitOfWork : IUnitOfWork
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaypalServices> _logger;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _contextAccessor;

    public UnitOfWork(IConfiguration configuration, ILogger<PaypalServices> logger, IEmailService emailService, IHttpContextAccessor contextAccessor)
    {
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _contextAccessor = contextAccessor;
        PaypalService = new PaypalServices(_configuration, _logger);
    }

    public IPaypalService PaypalService { get; private set; }

    public async Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory)
    {
        // Crear el producto
        var response = await PaypalService.CreateProductAsync(productName, productDescription, productType, productCategory);

        if (response.IsSuccessStatusCode)
        {
            // Obtener el email del usuario autenticado
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
''''