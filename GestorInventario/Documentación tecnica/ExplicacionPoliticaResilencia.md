**Documentacion de politica de resiliencia:**
## ¿Que son las politicas de resiliencia?
Las políticas de resiliencia son estrategias diseñadas para mejorar la confiabilidad y la robustez de una aplicación, especialmente en sistemas distribuidos donde las 
interacciones con servicios externos son frecuentes. Entre las estrategias más comunes se encuentran el `Circuit Breaker` y la `Retry Policy`. A continuación, se describen 
sus características, diferencias y manejadores de eventos asociados.
## Circuit Breaker
El Circuit Breaker es una estrategia que monitoriza una operación o servicio y "abre" el circuito (estado de fallo) para evitar que la aplicación realice más intentos si la 
operación sigue fallando. Cuando se alcanza un umbral definido de errores consecutivos o fallos, el Circuit Breaker se abre y bloquea futuras solicitudes durante un tiempo 
determinado, protegiendo así contra la sobrecarga de los sistemas subyacentes y evitando tiempos de espera prolongados o problemas persistentes.
Después de un período, el circuito entra en estado `half-open`, permitiendo algunas solicitudes de prueba para verificar si el problema ha sido resuelto. Si las solicitudes 
tienen éxito, el circuito se "cierra" y las operaciones se reanudan normalmente.

## Diferencias entre Circuit Breaker y Retry Policy

La `Retry Policy`  intenta reintentar operaciones fallidas de forma inmediata o con una espera programada entre intentos, mientras que el `Circuit Breaker`  se encarga de evitar que se 
realicen más intentos cuando se han acumulado suficientes fallos, para proteger el sistema. Ambas estrategias pueden combinarse para mejorar la resiliencia y la confiabilidad de una 
aplicación, gestionando fallos temporales y persistentes.
                 
## Manejadores de Eventos Circuit Braker:
**onBreak:** Se ejecuta cuando el circuito se abre. Registra la excepción que causó la apertura y la duración del tiempo que el circuito permanecerá abierto.
**onReset:** Se ejecuta cuando el circuito se cierra después de estar abierto, indicando que las operaciones han vuelto a la normalidad.
**onHalfOpen:** Se ejecuta cuando el circuito está en estado "half-open", permitiendo una solicitud de prueba para verificar si el problema ha sido resuelto.

## Manejadores de Eventos Retry Policy:
**retryCount: 5:** El número máximo de intentos de reintento que la política intentará antes de dejar de reintentar.
**sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)):** Proveedor de la duración de espera entre reintentos. Utiliza 
una estrategia de espera exponencial, donde el tiempo de espera aumenta exponencialmente con cada intento. Por ejemplo, los tiempos de espera 
serían 2^1 segundos,  2^2 segundos, 2^3 segundos, etc.
**onRetry:** Un manejador de eventos que se ejecuta en cada intento de reintento.
**onRetry:** Este manejador se ejecuta cada vez que ocurre una excepción que coincide con una de las excepciones especificadas y se va a realizar 
un reintento. El manejador onRetry tiene cuatro parámetros:
**exception:** La excepción que causó el reintento.
**calculatedWaitDuration:** La duración calculada de la espera antes de realizar el siguiente intento.
**attempt:** El número de intento actual.
**context:** Información adicional proporcionada al ejecutar la política..
## Ejemplo practico
```csharp
   public class PolicyHandler
    {
        private readonly ILogger<PolicyHandler> _logger;

        public PolicyHandler(ILogger<PolicyHandler> logger)
        {
            _logger = logger;
        }
      
     
      public IAsyncPolicy<T> GetCombinedPolicyAsync<T>()
      {
        var retryPolicy = Policy
            .Handle<SqlException>()
            .Or<DbUpdateException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                {
                    _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                });

        var circuitBreakerPolicy = Policy
            .Handle<SqlException>()
            .Or<DbUpdateException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, timespan, context) =>
                {
                    _logger.LogError(exception, $"Circuito abierto debido a una excepción: {exception.Message}. Duración: {timespan}", exception.Message, timespan);
                },
                onReset: (context) =>
                {
                    _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada será de prueba.");
                });

        var fallbackPolicy = Policy<T>
            .Handle<Exception>()
            .FallbackAsync(
                fallbackAction: async (context, cancellationToken) =>
                {
                    
                    _logger.LogError("Se ha producido un error. Se está ejecutando la acción de fallback.");
                    return Activator.CreateInstance<T>(); // Devuelve una nueva instancia del tipo T

                },
                onFallbackAsync: async (delegateResult, context) =>
                {
                    
                    _logger.LogError($"Error: {delegateResult.Exception.Message}. Se ha ejecutado la acción de fallback.");
                    await Task.CompletedTask; 
                });

        var combinedNonGenericPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        var combinedPolicy = fallbackPolicy.WrapAsync(combinedNonGenericPolicy);
        return combinedPolicy;
        }
        public Policy<T> GetCombinedPolicy<T>()
        {
            var retryPolicy = Policy
                .Handle<SqlException>()
                .Or<DbUpdateException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .WaitAndRetry(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                    {
                        _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                    });

            var circuitBreakerPolicy = Policy
                .Handle<SqlException>()
                .Or<DbUpdateException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan) =>
                    {
                        _logger.LogError(exception, $"Circuito abierto debido a una excepción: {exception.Message}. Duración: {timespan}", exception.Message, timespan);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada será de prueba.");
                    });

            var fallbackPolicy = Policy<T>
                .Handle<Exception>()
                .Fallback(
                    fallbackAction: context =>
                    {
                        
                        _logger.LogError("Se ha producido un error. Se está ejecutando la acción de fallback.");
                        return Activator.CreateInstance<T>(); // Devuelve una nueva instancia del tipo T

                    },
                    onFallback: (delegateResult, context) =>
                    {
                        
                        _logger.LogError($"Error: {delegateResult.Exception.Message}. Se ha ejecutado la acción de fallback.");
                    });

            var combinedNonGenericPolicy = Policy.Wrap(retryPolicy, circuitBreakerPolicy);
            var combinedPolicy = fallbackPolicy.Wrap(combinedNonGenericPolicy);
            return combinedPolicy;
        }

    }
    
  ```
 ## Explicacion de la clase PolicyHandler
La clase `PolicyHandler` es responsable de crear y gestionar políticas de resiliencia utilizando la biblioteca Polly. 
Esta clase contiene métodos para definir políticas de reintento, circuit breaker y fallback, así como para combinarlas en una sola política.
### Propiedades
- `_logger`: Un objeto de registro que se utiliza para registrar información sobre el estado de las políticas y cualquier error que ocurra durante su ejecución.
## Métodos
- `GetCombinedPolicyAsync<T>()`: Este método crea y devuelve una política combinada que incluye políticas de reintento, 
circuit breaker y fallback para operaciones asincrónicas.
## Explicacion del codigo Politica de reintento
```csharp
 var retryPolicy = Policy
                .Handle<SqlException>()
                .Or<DbUpdateException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .WaitAndRetry(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                    {
                        _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                    });
```
## Explicacion

- `retryPolicy`: variable que almacena la logica de esta politica 
- `.Handle<>() y .Or<>()`: que excepcion hara que salte esta politica el nombre de la excepcion se pone `<SqlException>`
- `.WaitAndRety`: metodo que se ejecuta cuando a ocurrido alguna excepcion tiene 3 propiedades.
- `retryCount`:reprenseta el numero de reintentos
- `sleepDurationProvider`: tiempo que espera entre cada reintento
- `onRetry`: metodo que se ejecuta cada vez que falla un intento
## Explicacion del codigo circuit bracker

```csharp
var circuitBreakerPolicy = Policy
                .Handle<SqlException>()
                .Or<DbUpdateException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan) =>
                    {
                        _logger.LogError(exception, $"Circuito abierto debido a una excepción: {exception.Message}. Duración: {timespan}", exception.Message, timespan);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada será de prueba.");
                    });
````
Tiene propiedades similar a la politica anterior con la diferencia en los metodos 
- `OnBreack`: que es cuando se abre el circuito al igual que el metodo anterior este se dispara cuando ocurre una excepcion
espera un poco y luego llama al metodo `onReset()`
- `onReset()`: este metodo se ejecuta cuando el circuito a estado abierto debido a un fallo ha terminado el tiempo de estar
abierto y cuando lo ha cerrado se ha resuelto la excepcion
- `onHalfOpen`: este metodo se ejecuta cuando falla los dos anteriores
## Explicacion politica fallback
```csharp
    var fallbackPolicy = Policy<T>
                .Handle<Exception>()
                .Fallback(
                    fallbackAction: context =>
                    {
                        
                        _logger.LogError("Se ha producido un error. Se está ejecutando la acción de fallback.");
                        return Activator.CreateInstance<T>(); // Devuelve una nueva instancia del tipo T

                    },
                    onFallback: (delegateResult, context) =>
                    {
                        
                        _logger.LogError($"Error: {delegateResult.Exception.Message}. Se ha ejecutado la acción de fallback.");
                    });
````
Este metodo se ejecuta cuando ocurre una excepcion no registrada o circuit bracker y retry policy fallan