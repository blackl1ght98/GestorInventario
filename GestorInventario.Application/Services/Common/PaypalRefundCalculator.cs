using System.Globalization;
using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal.BD;
using GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Application.Services.Common
{
    public static class PaypalRefundCalculator
    {
        
        public static OperationResult<RefundCalculadorDto> CalcularMontoDisponibleYEstado(
            PayPalOrderDetailsDto order,
            decimal montoSolicitado,
            string currency)
        {
            try
            {
                var capture = order.Captures.FirstOrDefault()
                              ?? throw new InvalidOperationException("La orden no contiene capturas de pago.");

                if (!string.Equals(currency, capture.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Moneda solicitada ({currency}) no coincide con la captura ({capture.Currency}).");
                }

                var grossAmount = capture.Amount;
                var refundedAmount = order.Refunds.Sum(r => r.Amount);
                var availableAmount = grossAmount - refundedAmount;

                if (availableAmount <= 0)
                {
                    return OperationResult<RefundCalculadorDto>.Fail(
                        $"No hay fondos disponibles. Bruto: {grossAmount}, Ya reembolsado: {refundedAmount}");
                }

                var finalRefundAmount = Math.Min(montoSolicitado, availableAmount);

                if (finalRefundAmount < montoSolicitado)
                {
                    return OperationResult<RefundCalculadorDto>.Fail(
                        $"Monto solicitado ({montoSolicitado}) excede disponible ({availableAmount}).");
                }

                var estado = finalRefundAmount >= availableAmount && refundedAmount == 0
                    ? "REFUNDED"
                    : "PARTIALLY_REFUNDED";

                return OperationResult<RefundCalculadorDto>.Ok(new RefundCalculadorDto
                {
                    MontoDisponible = availableAmount,
                    MontoReembolso = finalRefundAmount,
                    Estado = estado
                });
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult<RefundCalculadorDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<RefundCalculadorDto>.Fail($"Error inesperado: {ex.Message}");
            }
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
