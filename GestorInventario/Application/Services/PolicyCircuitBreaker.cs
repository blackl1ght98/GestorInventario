using Polly.CircuitBreaker;
using Polly;
using Microsoft.Data.SqlClient;

namespace GestorInventario.Application.Services
{
    public class PolicyCircuitBreaker
    {
       //Para dudas mirar ExplicacionPoliticaResilencia.txt
        private readonly ILogger<PolicyCircuitBreaker> _logger;

        public PolicyCircuitBreaker(ILogger<PolicyCircuitBreaker> logger)
        {
            _logger = logger;
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
    }
}
