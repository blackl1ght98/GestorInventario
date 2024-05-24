using GestorInventario.Domain.Models;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;

namespace GestorInventario.Application.Services
{
    public class RetryPolicyHandler
    {
        private readonly ILogger<RetryPolicyHandler> _logger;

        public RetryPolicyHandler(ILogger<RetryPolicyHandler> logger)
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






    }
}
