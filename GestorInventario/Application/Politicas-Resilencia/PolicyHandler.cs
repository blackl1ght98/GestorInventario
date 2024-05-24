using Microsoft.Data.SqlClient;
using Polly.Retry;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Politicas_Resilencia
{
    public class PolicyHandler
    {
        private readonly ILogger<PolicyHandler> _logger;

        public PolicyHandler(ILogger<PolicyHandler> logger)
        {
            _logger = logger;
        }
        public Policy GetRetryPolicy()
        {
            return Policy
                .Handle<SqlException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .WaitAndRetry(
                    retryCount: 5, // Número de reintentos
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // Retroceso exponencial
                    onRetry: (exception, calculatedWaitDuration, attempt) =>
                    {
                        _logger.LogWarning($"Error: {exception.Message}. Esperando {calculatedWaitDuration}. Numero de reintentos {attempt}");
                    });
        }
        public AsyncRetryPolicy GetRetryPolicyAsync()
        {
            return Policy
                //Excepciones que maneja la politica de reintento
                .Handle<SqlException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                //Si una de las excepcione temporales ocurre
                .WaitAndRetryAsync(
                    retryCount: 5, // Disminulle en 1 el numero de reintentos
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // cuanto tiempo va a tardaar en responder normalmente se pone un tiempo corto
                    onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                    {
                        // Registra la excepcion
                        _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                    });
        }
        public CircuitBreakerPolicy GetCircuitBreakerPolicy()
        {
            return Policy
                .Handle<SqlException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan) =>
                    {
                        _logger.LogError(exception, "Circuito abierto debido a una excepción: {Exception}. Duración: {Timespan}", exception.Message, timespan);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada será de prueba.");
                    });
        }
        public AsyncCircuitBreakerPolicy GetCircuitBreakerPolicyAsync()
        {
            return Policy
                .Handle<SqlException>()
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan, context) =>
                    {
                        _logger.LogError(exception, "Circuito abierto debido a una excepción: {Exception}. Duración: {Timespan}", exception.Message, timespan);
                    },
                    onReset: (context) =>
                    {
                        _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada será de prueba.");
                    });
        }
        public IAsyncPolicy GetCombinedPolicyAsync()
        {
            var retryPolicy = Policy
                .Handle<SqlException>()
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
                .Or<TimeoutException>()
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, timespan, context) =>
                    {
                        _logger.LogError(exception, "Circuito abierto debido a una excepción: {Exception}. Duración: {Timespan}", exception.Message, timespan);
                    },
                    onReset: (context) =>
                    {
                        _logger.LogInformation("Circuito cerrado. Operaciones normalizadas.");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuito en estado Half-Open: La siguiente llamada será de prueba.");
                    });

            var fallbackPolicy = Policy
             .Handle<Exception>()
             .FallbackAsync(
             fallbackAction: async (context, cancellationToken) =>
             {
                 // Acción a realizar en caso de fallo
                 _logger.LogError("Se ha producido un error. Se está ejecutando la acción de fallback.");
                 await Task.CompletedTask; // Si no hay ninguna operación asíncrona, usa Task.CompletedTask
             },
             onFallbackAsync: async (delegateResult, context) =>
             {
                 // Registro del error
                 _logger.LogError($"Error: {delegateResult.Message}. Se ha ejecutado la acción de fallback.");
                 await Task.CompletedTask; // Si no hay ninguna operación asíncrona, usa Task.CompletedTask
             });


            return Policy.WrapAsync(fallbackPolicy, retryPolicy, circuitBreakerPolicy);
        }










    }

}


