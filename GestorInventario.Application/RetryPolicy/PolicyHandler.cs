using GestorInventario.Interfaces.Application.RetryPolicy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace GestorInventario.Application.RetryPolicy
{
    public class PolicyHandler: IPolicyHandler
    {
        private readonly ILogger<PolicyHandler> _logger;

        public PolicyHandler(ILogger<PolicyHandler> logger)
        {
            _logger = logger;
        }
        private static PolicyBuilder BuildSqlAndHttpExceptions() =>
        Policy.Handle<SqlException>()
          .Or<DbUpdateException>()
          .Or<TimeoutException>()
          .Or<HttpRequestException>();
        public IAsyncPolicy<T> GetCombinedPolicyAsync<T>()
        {
            var retryPolicy = BuildSqlAndHttpExceptions()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                    {
                        _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                    });

            var circuitBreakerPolicy = BuildSqlAndHttpExceptions()
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
                        return Activator.CreateInstance<T>();
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
            var retryPolicy = BuildSqlAndHttpExceptions()
                .WaitAndRetry(
                    retryCount: 5,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, calculatedWaitDuration, attempt, context) =>
                    {
                        _logger.LogWarning($"Error: {exception.Message}. Waiting {calculatedWaitDuration} before next retry. Retry attempt {attempt}");
                    });

            var circuitBreakerPolicy = BuildSqlAndHttpExceptions()
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
                        return Activator.CreateInstance<T>();
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
}