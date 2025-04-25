**Documentacion de politica de resiliencia:**
## �Que son las politicas de resiliencia?
Las pol�ticas de resiliencia son estrategias dise�adas para mejorar la confiabilidad y la robustez de una aplicaci�n, especialmente en sistemas distribuidos donde las 
interacciones con servicios externos son frecuentes. Entre las estrategias m�s comunes se encuentran el `Circuit Breaker` y la `Retry Policy`. A continuaci�n, se describen 
sus caracter�sticas, diferencias y manejadores de eventos asociados.
## Circuit Breaker
El Circuit Breaker es una estrategia que monitoriza una operaci�n o servicio y "abre" el circuito (estado de fallo) para evitar que la aplicaci�n realice m�s intentos si la 
operaci�n sigue fallando. Cuando se alcanza un umbral definido de errores consecutivos o fallos, el Circuit Breaker se abre y bloquea futuras solicitudes durante un tiempo 
determinado, protegiendo as� contra la sobrecarga de los sistemas subyacentes y evitando tiempos de espera prolongados o problemas persistentes.
Despu�s de un per�odo, el circuito entra en estado `half-open`, permitiendo algunas solicitudes de prueba para verificar si el problema ha sido resuelto. Si las solicitudes 
tienen �xito, el circuito se "cierra" y las operaciones se reanudan normalmente.

## Diferencias entre Circuit Breaker y Retry Policy

La `Retry Policy`  intenta reintentar operaciones fallidas de forma inmediata o con una espera programada entre intentos, mientras que el `Circuit Breaker`  se encarga de evitar que se 
realicen m�s intentos cuando se han acumulado suficientes fallos, para proteger el sistema. Ambas estrategias pueden combinarse para mejorar la resiliencia y la confiabilidad de una 
aplicaci�n, gestionando fallos temporales y persistentes.
                 
## Manejadores de Eventos Circuit Braker:
**onBreak:** Se ejecuta cuando el circuito se abre. Registra la excepci�n que caus� la apertura y la duraci�n del tiempo que el circuito permanecer� abierto.
**onReset:** Se ejecuta cuando el circuito se cierra despu�s de estar abierto, indicando que las operaciones han vuelto a la normalidad.
**onHalfOpen:** Se ejecuta cuando el circuito est� en estado "half-open", permitiendo una solicitud de prueba para verificar si el problema ha sido resuelto.

## Manejadores de Eventos Retry Policy:
**retryCount: 5:** El n�mero m�ximo de intentos de reintento que la pol�tica intentar� antes de dejar de reintentar.
**sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)):** Proveedor de la duraci�n de espera entre reintentos. Utiliza 
una estrategia de espera exponencial, donde el tiempo de espera aumenta exponencialmente con cada intento. Por ejemplo, los tiempos de espera 
ser�an 2^1 segundos,  2^2 segundos, 2^3 segundos, etc.
**onRetry:** Un manejador de eventos que se ejecuta en cada intento de reintento.
**onRetry:** Este manejador se ejecuta cada vez que ocurre una excepci�n que coincide con una de las excepciones especificadas y se va a realizar 
un reintento. El manejador onRetry tiene cuatro par�metros:
**exception:** La excepci�n que caus� el reintento.
**calculatedWaitDuration:** La duraci�n calculada de la espera antes de realizar el siguiente intento.
**attempt:** El n�mero de intento actual.
**context:** Informaci�n adicional proporcionada al ejecutar la pol�tica..
## Ejemplo practico
 public IAsyncPolicy<T> GetCombinedPolicyAsync<T>()
   {
   **Policy**:Clase del paquete nuget **Polly** encargada de capturar las excepciones.
     var retryPolicy = Policy
     **.Handle<>():** Dentro de `<>` se especifica el tipo de excepcion que activara la politica de reintento, si se quiere especificar que maneje mas de una excepcion se pone
     **.Or<>():** y la excepcion se pone dentro de `<>` se puede poner tantos .Or<>() como sean necesarios
            .Handle<SqlException>()
            .Or<DbUpdateException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            **.WaitAndRetryAsync():** metodo de polly que se encarga de que va a pasar si ocurre alguna excepcion de las anteriores, este metodo tiene varios parametros para manejar
            el que ocurrira. A continuaci�n vamos a verlo:
                    - **retryCount:** el numero maximo de reintentos que la politica intentara antes de dejar de reintentar 
                    - **sleepDurationProvider:attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))** El tiempo que pasa entre reintento este tiempo se incrementa de forma 
                    exponencial dandole asi tiempo al servidor. Por ejemplo al primer reintento 2^1 segundos, para el segundo reintento seria 2^2 segundos y asi hasta llegar
                    ha 5 reintentos.
                    - **onRetry:** esta parte se ejecuta cuando falla registrando la excepcion y el numero y tiempo de reintentos empleado para dejar de fallar
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                {
                    _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                });
Si la politica anterior falla pasa a ejecutar la politica `circuit breaker`
        var circuitBreakerPolicy = Policy
        Las excepciones que se quiere que maneje se registra de la misma forma que el anterior.
            .Handle<SqlException>()
            .Or<DbUpdateException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            **CircuitBreakerAsync():**metodo de polly que se encarga de manejar que ocurre si el circuito se abre. A continuacion vamos a ver que metodos tiene:
                - **exceptionsAllowedBeforeBreaking:**El numero de intentos que se va ha abrir el circuito entre fallo y fallo.
                - **durationOfBreak:** el tiempo de espera que se va a esperar cuando el circuito se abre.
                - **onBreak:** si el circuito se abre la excepcion el tiempo y el mensaje de la excepcion son registrados.
                - **onReset:** cuando el circuito se cierra y vuelve a funcionar normal.
                - **onHalfOpen:** cuando falla se registra esta excepcion  y si las dos politicas fallan pasa a la politica fallback
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, timespan, context) =>
                {
                    _logger.LogError(exception, $"Circuito abierto debido a una excepci�n: {exception.Message}. Duraci�n: {timespan}", exception.Message, timespan);
                },
                onReset: (context) =>
                {
                    _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada ser� de prueba.");
                });
La politica de fallback se ejecuta en caso de que las dos politicas anteriores fallen o se de el caso de que de una excepcion no registrada
        var fallbackPolicy = Policy<T>
            .Handle<Exception>()
            El metodo **FallbackAsync** es el encargado de llevar a cabo las acciones en caso de fallo y este metodo tiene dos propiedades:
                - **fallbackAction:** lo que ocurre si se ejecuta la politica fallback.
                - **onFallbackAsync:** registra la excepcion que causo la ejecucion de fallback
            .FallbackAsync(
                fallbackAction: async (context, cancellationToken) =>
                {
                    
                    _logger.LogError("Se ha producido un error. Se est� ejecutando la acci�n de fallback.");
                    return default(T);
                },
                onFallbackAsync: async (delegateResult, context) =>
                {
                    // Registro del error
                    _logger.LogError($"Error: {delegateResult.Exception.Message}. Se ha ejecutado la acci�n de fallback.");
                    await Task.CompletedTask; // Si no hay ninguna operaci�n as�ncrona, usa Task.CompletedTask
                });
Para que los metodos anteriores se metan aqui en caso de fallo, los dos metodos se combinan usando ` Policy.WrapAsync(retryPolicy, circuitBreakerPolicy)`
y luego se crea otra variable que llama a `fallback` y la combinacion anterior `fallbackPolicy.WrapAsync(combinedNonGenericPolicy)` y se devuelve 
las politicas combinadas con el `fallback`
        var combinedNonGenericPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        var combinedPolicy = fallbackPolicy.WrapAsync(combinedNonGenericPolicy);
        return combinedPolicy;
        }
## �Como llamar a estas politicas desde el controlador?
**Func<Task<T>>** Encapsula un metodo que no tiene parametros y devuelve un valor especificado `<T>` en este caso `Func` encapsula a `Func<Task<T>>` una operaci�n asincrona `Task<T>`
que con el uso de los genericos `<T>` admite cualquier tipo de dato esto es util cuando se usa la politica para las consultas a base de datos y operaciones con base de datos.
 private async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicyAsync<T>();
            **return await policy.ExecuteAsync(operation)** se devuelve la operacion con la politica aplicada
            return await policy.ExecuteAsync(operation);
        }
        private T ExecutePolicy<T>(Func<T> operation)
        {
            var policy = _PolicyHandler.GetCombinedPolicy<T>();
            return policy.Execute(operation);
        }