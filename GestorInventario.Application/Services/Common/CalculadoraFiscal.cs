using System.Globalization;

namespace GestorInventario.Application.Services.Common
{
    public static class CalculadoraFiscal
    {
        public const decimal TASA_IVA = 0.21m;

        /// <summary>
        /// Calcula subtotal, IVA y total a partir de líneas de pedido
        /// </summary>
        public static (decimal subtotal, decimal iva, decimal total) CalcularTotales(
            IEnumerable<(decimal precioUnitario, int cantidad)> lineas)
        {
            decimal subtotal = lineas.Sum(l => l.precioUnitario * l.cantidad);
            decimal iva = Math.Round(subtotal * TASA_IVA, 2);
            decimal total = subtotal + iva;
            return (subtotal, iva, total);
        }

        /// <summary>
        /// Calcula el IVA de un precio unitario
        /// </summary>
        public static decimal CalcularIvaUnitario(decimal precioSinIva) =>
            Math.Round(precioSinIva * TASA_IVA, 2);

        /// <summary>
        /// Formatea un decimal para PayPal (siempre 2 decimales, punto como separador)
        /// </summary>
        public static string FormatearPayPal(decimal valor) =>
            valor.ToString("F2", CultureInfo.InvariantCulture);
    }
}
