using GestorInventario.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class HistorialProductosPdf : IDocument
    {
        private readonly List<HistorialProducto> _data;

        public HistorialProductosPdf(List<HistorialProducto> data)
        {
            _data = data;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.Margin(20);

                page.Header().Text("REPORTE DE HISTORIAL DE PRODUCTOS")
                    .AlignCenter()
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                page.Content().Element(ComposeContent);

                page.Footer().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x
                        .FontSize(9)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1));

                    text.Span($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingTop(20).Column(col =>
            {
                foreach (var h in _data)
                {
                    col.Item().Element(c => HistorialTable(c, h));
                    col.Item().PaddingVertical(10);
                }
            });
        }

        #region Tabla principal

        void HistorialTable(IContainer container, HistorialProducto h)
        {
            container.Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    HeaderCell(table, "Id");
                    HeaderCell(table, "UsuarioId");
                    HeaderCell(table, "Fecha");
                    HeaderCell(table, "Acción");
                    HeaderCell(table, "IP");

                    DataCell(table, h.Id.ToString());
                    DataCell(table, h.UsuarioId.ToString());
                    DataCell(table, h.Fecha.ToString());
                    DataCell(table, h.Accion);
                    DataCell(table, h.Ip);
                });

                if (h.DetalleHistorialProductos.Any())
                {
                    col.Item().PaddingTop(5)
                        .Element(c => DetalleTable(c, h.DetalleHistorialProductos.ToList()));
                }
            });
        }

        #endregion

        #region Tabla detalles

        void DetalleTable(IContainer container, List<DetalleHistorialProducto> detalles)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                DetailHeader(t, "NombreProducto");
                DetailHeader(t, "Descripción");
                DetailHeader(t, "IdHistorial");
                DetailHeader(t, "Cantidad");
                DetailHeader(t, "Precio");

                foreach (var d in detalles)
                {
                    DetailCellCenter(t, d.NombreProducto ?? "N/A");
                    DetailCellCenter(t, d.Descripcion ?? "N/A");
                    DetailCellCenter(t, d.HistorialProductoId.ToString());
                    DetailCellCenter(t, d.Cantidad.ToString());
                    DetailCellCenter(t, d.Precio.ToString());
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

        void DetailCellCenter(TableDescriptor table, string text)
        {
            table.Cell().Padding(3)
                .AlignMiddle()
                .AlignCenter()
                .Text(text)
                .FontSize(7)
                .FontColor(Colors.Grey.Darken2);
        }

        #endregion
    }
}