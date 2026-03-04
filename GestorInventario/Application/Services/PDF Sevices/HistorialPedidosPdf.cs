using GestorInventario.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class HistorialPedidosPdf : IDocument
    {
        private readonly List<HistorialPedido> _data;

        public HistorialPedidosPdf(List<HistorialPedido> data)
        {
            _data = data;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container
                .AlignCenter()
                .Text("REPORTE DE HISTORIAL DE PEDIDOS")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken2);
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingTop(20).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(1.5f);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(3);
                });

                Header(table);

                foreach (var h in _data)
                {
                    Row(table, h);
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x
                    .FontSize(9)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1));

                text.Span($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm} - ");
                text.Span($"Total registros: {_data.Count}");
            });
        }


        #region Tabla principal

        void Header(TableDescriptor table)
        {
            HeaderCell(table, "Id");
            HeaderCell(table, "Acción");
            HeaderCell(table, "IP");
            HeaderCell(table, "Id Usuario");
            HeaderCell(table, "Fecha");
            HeaderCell(table, "Detalles");
        }

        void Row(TableDescriptor table, HistorialPedido h)
        {
            DataCell(table, h.Id.ToString());
            DataCell(table, h.Accion);
            DataCell(table, h.Ip);
            DataCell(table, h.IdUsuario.ToString());
            DataCell(table, h.Fecha.ToString());

            table.Cell().Element(c =>
            {
                if (h.DetalleHistorialPedidos.Any())
                    DetalleTable(c, h.DetalleHistorialPedidos.ToList());
                else
                    c.Text("Sin detalles")
                     .FontSize(8)
                     .Italic()
                     .FontColor(Colors.Grey.Darken1);
            });
        }

        #endregion

        #region Tabla detalles

        void DetalleTable(IContainer container, List<DetalleHistorialPedido> detalles)
        {
            container.Padding(5).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(2);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1.5f);
                    c.RelativeColumn(1.5f);
                });

                DetailHeader(t, "ID Producto");
                DetailHeader(t, "Producto");
                DetailHeader(t, "Cantidad");
                DetailHeader(t, "Estado");
                DetailHeader(t, "N° Pedido");

                foreach (var d in detalles)
                {
                    DetailCellCenter(t, d.ProductoId.ToString()?? "N/A");
                    DetailCellCenter(t, d.Producto?.NombreProducto ?? "N/A");
                    DetailCellCenter(t, d.Cantidad.ToString() ?? "N/A");
                    DetailCellCenter(t, d.EstadoPedido ?? "N/A");
                    DetailCellCenter(t, d.NumeroPedido ?? "N/A");
                }
            });
        }

        #endregion

        #region Estilos

        void HeaderCell(TableDescriptor table, string text)
        {
            table.Cell().Background(Colors.Blue.Darken2).Padding(5)
                .AlignCenter().AlignMiddle()
                .Text(text).FontSize(10).Bold().FontColor(Colors.White);
        }

        void DataCell(TableDescriptor table, string text)
        {
            table.Cell().Padding(5)
                .AlignCenter().AlignMiddle()
                .Text(text).FontSize(9);
        }

        void DetailHeader(TableDescriptor table, string text)
        {
            table.Cell().Background(Colors.Grey.Lighten3).Padding(3)
                .AlignCenter().AlignMiddle()
                .Text(text).FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
        }

        void DetailCellLeft(TableDescriptor table, string text)
        {
            table.Cell().Padding(3)
                .AlignMiddle()
                .Text(text)
                .AlignLeft()
                .FontSize(7)
                .FontColor(Colors.Grey.Darken2);
        }

        void DetailCellCenter(TableDescriptor table, string text)
        {
            table.Cell().Padding(3)
                .AlignMiddle()
                .Text(text)
                .AlignCenter()
                .FontSize(7)
                .FontColor(Colors.Grey.Darken2);
        }



        #endregion
    }

}
