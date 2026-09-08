using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GestorInventario.Application.Services.Common
{
    public static class PaypalRefundCalculator
    {
        public static (decimal montoReembolso, decimal montoDisponible, string estadoVenta)
        CalcularMontoDisponibleYEstado(
            OrderDetailsResponse captureDetails,
            decimal montoSolicitado,
            string currency,
            ILogger logger)
        {
            var firstUnit = captureDetails.PurchaseUnits?.FirstOrDefault()
                ?? throw new InvalidOperationException("La orden no contiene unidades de compra.");

            var capture = firstUnit.Payments?.Captures?.FirstOrDefault()
                ?? throw new InvalidOperationException("La orden no contiene capturas de pago.");

            if (currency != capture.Amount?.CurrencyCode)
                throw new InvalidOperationException(
                    $"Moneda solicitada ({currency}) no coincide con la captura ({capture.Amount?.CurrencyCode}).");

            var grossAmount = ParseDecimalSeguro(capture.Amount?.Value, "monto bruto de la captura");

            var refundedAmount = firstUnit.Payments?.Refunds?
                .Where(r => r.Amount?.Value != null)
                .Sum(r => ParseDecimalSeguro(r.Amount.Value, "monto de reembolso previo"))
                ?? 0m;

            var availableAmount = grossAmount - refundedAmount;

            if (availableAmount <= 0)
            {
                logger.LogWarning("No hay fondos disponibles para reembolsar. Bruto: {Gross}, Ya reembolsado: {Refunded}",
                    grossAmount, refundedAmount);
                throw new InvalidOperationException("No hay monto disponible para reembolsar.");
            }

            var finalRefundAmount = Math.Min(montoSolicitado, availableAmount);

            if (finalRefundAmount < montoSolicitado)
            {
                logger.LogWarning(
                    "Monto solicitado ({Solicitado}) excede disponible ({Disponible}). Ajustando a {Ajustado}.",
                    montoSolicitado, availableAmount, finalRefundAmount);
            }

            var estadoVenta = finalRefundAmount >= availableAmount && refundedAmount == 0
                ? "REFUNDED"
                : "PARTIALLY_REFUNDED";

            return (finalRefundAmount, availableAmount, estadoVenta);
        }

        private static decimal ParseDecimalSeguro(string? value, string campo)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"El campo '{campo}' no contiene un valor válido.");

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                throw new InvalidOperationException($"No se pudo parsear el campo '{campo}': {value}");

            return result;
        }
    }
}
