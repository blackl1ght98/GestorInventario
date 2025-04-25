# Gu�a de Uso del Patr�n Unit of Work

## Introducci�n

El patr�n **Unit of Work** (Unidad de Trabajo) es un patr�n de dise�o que se utiliza principalmente en aplicaciones que interact�an con bases de datos, pero tambi�n 
puede aplicarse a otros contextos donde se necesita coordinar la interacci�n entre varios servicios. El objetivo principal del patr�n es centralizar la l�gica de las 
operaciones que deben ejecutarse de manera conjunta, permitiendo una mejor organizaci�n del c�digo y facilitando la gesti�n de transacciones.

## �Por Qu� Usar el Patr�n Unit of Work?

1. **Coordinaci�n de M�ltiples Servicios**: Cuando tu aplicaci�n necesita realizar operaciones que involucran varios servicios (por ejemplo, crear un producto y 
 enviar un correo electr�nico), el patr�n Unit of Work ayuda a coordinar estas operaciones en un solo punto.

2. **Gesti�n de Transacciones**: Aunque en este caso no estamos interactuando directamente con una base de datos, el patr�n se inspira en la gesti�n de transacciones. 
 La idea es asegurar que todas las operaciones se completen correctamente antes de confirmar el "commit" de la transacci�n. Si algo falla, se puede "deshacer" todo lo 
 que se hizo hasta ese punto.

3. **Mantenibilidad y Extensibilidad**: Centralizar la l�gica de coordinaci�n de servicios en un solo lugar facilita la mantenibilidad del c�digo. Adem�s, si en el futuro 
 se necesita a�adir m�s servicios (como notificaciones por SMS, registros en logs, etc.), se pueden integrar f�cilmente en el Unit of Work.

## Ejemplo de Implementaci�n

A continuaci�n, se presenta un ejemplo de c�mo implementar un Unit of Work para coordinar la creaci�n de un producto en PayPal y el env�o de un correo electr�nico de notificaci�n.

### C�digo de Ejemplo

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
''''