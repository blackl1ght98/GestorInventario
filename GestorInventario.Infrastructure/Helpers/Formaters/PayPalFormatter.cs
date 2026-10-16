using System.Globalization;

namespace GestorInventario.Infrastructure.Helpers.Formaters;

public static class PayPalFormatter
{
    /// <summary>
    /// Formatea un decimal para PayPal (siempre 2 decimales, punto como separador)
    /// </summary>
    public static string FormatearPayPal(decimal valor) =>
        valor.ToString("F2", CultureInfo.InvariantCulture);
}