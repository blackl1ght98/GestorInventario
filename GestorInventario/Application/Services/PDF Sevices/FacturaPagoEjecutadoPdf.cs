using GestorInventario.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class FacturaPagoEjecutadoPdf : IDocument
    {
        private readonly PayPalPaymentDetail _data;

        public FacturaPagoEjecutadoPdf(PayPalPaymentDetail data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("GestorInventario")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Factura de Pago PayPal")
                        .FontSize(14).SemiBold();
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text($"Fecha: {_data.CreateTime:dd/MM/yyyy HH:mm}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().Text($"ID Pago: {_data.Id}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(col =>
            {
                col.Item().Element(ComposeClientDetails);
                col.Item().PaddingVertical(15);
                col.Item().Element(ComposeTransactionDetails);
                col.Item().PaddingVertical(15);
                col.Item().Element(ComposeItemsTable);
                col.Item().PaddingVertical(15);
                col.Item().Element(ComposeTotals);
                col.Item().PaddingVertical(15);
                col.Item().Element(ComposeAdditionalDetails);
            });
        }

        private void ComposeClientDetails(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("Detalles del Cliente").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    AddDetailRow(table, "Nombre", $"{_data.PayerFirstName} {_data.PayerLastName}");
                    AddDetailRow(table, "Email", _data.PayerEmail ?? "No disponible");
                    AddDetailRow(table, "Dirección de envío",
                        $" {_data.PayPalPaymentShippings
    .FirstOrDefault()?.AddressLine1},{_data.PayPalPaymentShippings
    .FirstOrDefault()?.City},{_data.PayPalPaymentShippings
    .FirstOrDefault()?.State}");
                });
            });
        }

        private void ComposeTransactionDetails(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("Detalles de la Transacción").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    AddDetailRow(table, "Estado", _data.Status ?? "N/A");
                    AddDetailRow(table, "Descripción", _data.Description ?? "N/A");
                    AddDetailRow(table, "ID de Venta/Captura", _data.PayPalPaymentCaptures.FirstOrDefault()?.CaptureId ?? "N/A");
                    AddDetailRow(table, "Estado de Captura", _data.PayPalPaymentCaptures.FirstOrDefault()?.Status ?? "N/A");
                });
            });
        }

        private void ComposeItemsTable(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("Productos Comprados").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().AlignMiddle().Text("Producto").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().AlignMiddle().Text("Cantidad").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().AlignMiddle().Text("Precio Unitario").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().AlignMiddle().Text("Impuesto").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().AlignMiddle().Text("Total").FontSize(10).Bold().FontColor(Colors.White);
                    });

                    if (_data.PayPalPaymentItems != null && _data.PayPalPaymentItems.Any())
                    {
                        foreach (var item in _data.PayPalPaymentItems)
                        {
                            table.Cell().Padding(5).AlignLeft().AlignMiddle().Text(item.ItemName ?? "N/A");
                            table.Cell().Padding(5).AlignCenter().AlignMiddle().Text(item.ItemQuantity?.ToString() ?? "N/A");
                            table.Cell().Padding(5).AlignRight().AlignMiddle().Text($"{item.ItemPrice?.ToString("C") ?? "N/A"} {item.ItemCurrency}");
                            table.Cell().Padding(5).AlignRight().AlignMiddle().Text($"{item.ItemTax?.ToString("C") ?? "0.00"} {item.ItemCurrency}");
                            table.Cell().Padding(5).AlignRight().AlignMiddle().Text($"{(item.ItemPrice * item.ItemQuantity + item.ItemTax)?.ToString("C") ?? "N/A"} {item.ItemCurrency}");
                        }
                    }
                    else
                    {
                        table.Cell().ColumnSpan(5).Padding(10).AlignCenter().AlignMiddle().Text("No hay ítems registrados").Italic().FontColor(Colors.Grey.Medium);
                    }
                });
            });
        }

        private void ComposeTotals(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("Totales").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    AddTotalRow(table, "Subtotal ítems", $"{_data.AmountItemTotal?.ToString("C") ?? "0.00"} {_data.AmountCurrency}");
                    AddTotalRow(table, "Envío", $"{_data.AmountShipping?.ToString("C") ?? "0.00"} {_data.AmountCurrency}");
                    AddTotalRow(table, "Total", $"{_data.AmountTotal?.ToString("C") ?? "0.00"} {_data.AmountCurrency}", isBold: true);
                });
            });
        }

        private void ComposeAdditionalDetails(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text("Detalles Adicionales").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    AddDetailRow(table, "Comisión PayPal", $"{_data.PayPalPaymentCaptures.FirstOrDefault()?.TransactionFeeAmount?.ToString("C") ?? "0.00"} {_data.PayPalPaymentCaptures.FirstOrDefault()?.TransactionFeeCurrency}");
                    AddDetailRow(table, "Tasa de cambio", _data.PayPalPaymentCaptures.FirstOrDefault()?.ExchangeRate?.ToString("F4") ?? "N/A");
                    AddDetailRow(table, "Monto recibible", $"{_data.PayPalPaymentCaptures.FirstOrDefault()?.ReceivableAmount?.ToString("C") ?? "0.00"} {_data.PayPalPaymentCaptures.FirstOrDefault()?.ReceivableCurrency}");
                    AddDetailRow(table, "ID de Seguimiento", _data.TrackingId ?? "N/A");
                    AddDetailRow(table, "Estado de Seguimiento", _data.TrackingStatus ?? "N/A");
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingVertical(10).Row(row =>
            {
                row.RelativeItem().AlignLeft().Text("GestorInventario © 2026")
                    .FontSize(9).FontColor(Colors.Grey.Medium);

                row.RelativeItem().AlignRight().Text($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }

       

        private static void AddDetailRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Padding(5).Background(Colors.Grey.Lighten3).Text(label).SemiBold();
            table.Cell().Padding(5).Text(value);
        }

        private static void AddTotalRow(TableDescriptor table, string label, string value, bool isBold = false)
        {
            table.Cell().Padding(5).Background(Colors.Grey.Lighten2).Text(label).SemiBold();
            var cell = table.Cell().Padding(5).AlignRight();
            if (isBold)
               
            cell.Text(value).FontColor(Colors.Black);
        }
    }
}