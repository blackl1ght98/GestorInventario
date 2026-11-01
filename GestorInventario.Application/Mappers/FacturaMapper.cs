using GestorInventario.Application.DTOS;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Mappers;

public static class FacturaMapper
    {
        public static FacturaPdfDto ToDto(PayPalPaymentDetail data)
        {
            if (data == null) return null;

            var ultimoCapture = data.PayPalPaymentCaptures?
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            var primerEnvio = data.PayPalPaymentShippings?.FirstOrDefault();

            return new FacturaPdfDto
            {
                IdPago = data.Id,
                FechaCreacion = data.CreateTime,
                OrderStatus = data.OrderStatus,
                Description = data.Description,

                PayerNombreCompleto = $"{data.PayerFirstName} {data.PayerLastName}",
                PayerEmail = data.PayerEmail ?? "No disponible",
                DireccionEnvio = primerEnvio != null 
                    ? $"{primerEnvio.AddressLine1}, {primerEnvio.City}, {primerEnvio.State}" 
                    : "No disponible",

                UltimoCaptureId = ultimoCapture?.CaptureId ?? "N/A",
                UltimoCaptureStatus = ultimoCapture?.Status ?? "N/A",
                TransaccionFeeAmount = ultimoCapture?.TransactionFeeAmount ?? 0,
                TransaccionFeeCurrency = ultimoCapture?.TransactionFeeCurrency,
                ExchangeRate = ultimoCapture?.ExchangeRate ?? 0,
                ReceivableAmount = ultimoCapture?.ReceivableAmount ?? 0,
                ReceivableCurrency = ultimoCapture?.ReceivableCurrency,

                AmountItemTotal = data.AmountItemTotal,
                AmountShipping = data.AmountShipping,
                AmountTotal = data.AmountTotal,
                AmountCurrency = data.AmountCurrency,

                Items = data.PayPalPaymentItems?
                    .Select(item => new FacturaItemDto
                    {
                        ItemName = item.ItemName,
                        ItemQuantity = item.ItemQuantity,
                        ItemPrice = item.ItemPrice,
                        ItemTax = item.ItemTax,
                        ItemCurrency = item.ItemCurrency
                    }).ToList() ?? new()
            };
        }
    }