using System.Globalization;

namespace GestorInventario.Application.Services.Common
{
    public static class CalculadoraFiscal
    {
        public const decimal TASA_IVA = 0.21m;

        /// <summary>
        ///  Calcula el total a pagar de un pedido completo (todas sus prductos juntos).

        /// Ejemplo con dos productos: Monitor (50 x 2) y Smart TV (200 x 1)
        /// 1º Se suman los subtotales sin IVA de cada línea: (50*2) + (200*1) = 100 + 200 = 300
        /// 2º Se calcula el IVA UNA SOLA VEZ sobre ese total: 300 * 0.21 = 63
        /// 3º Se suma para obtener el total con IVA: 300 + 63 = 363
        
        /// </summary>
        public static (decimal subtotalSinIva, decimal iva, decimal totalConIva) CalcularTotales(
            IEnumerable<(decimal precioUnitario, int cantidad)> productos)
        {
            
            decimal subtotalSinIva = productos.Sum(l => l.precioUnitario * l.cantidad);
         
            decimal iva = CalcularIvaUnitario(subtotalSinIva);
            decimal totalConIva = subtotalSinIva + iva;
            return (subtotalSinIva, iva, totalConIva);
        }
        /// <summary>
        /// Calcula cuánto cuesta un único producto dentro de un pedido.
        /// </summary>
        public static (decimal subtotalSinIva, decimal iva, decimal totalConIva) CalcularCosteProducto(
            decimal precioUnitario, int cantidad)
        {
            decimal subtotalSinIva = precioUnitario * cantidad;
            decimal iva = CalcularIvaUnitario(subtotalSinIva);
            decimal totalConIva = subtotalSinIva + iva;

            return (subtotalSinIva, iva, totalConIva);
        }
        /// <summary>
        /// Calcula el IVA de un precio unitario
        /// </summary>
        public static decimal CalcularIvaUnitario(decimal precioSinIva) =>
            Math.Round(precioSinIva * TASA_IVA, 2);
      
        /// <summary>
        /// Dado un precio sin IVA, devuelve el precio con IVA incluido
        /// </summary>
        public static decimal CalcularPrecioConIva(decimal precioSinIva)
        {
            decimal iva = CalcularIvaUnitario(precioSinIva);
            return precioSinIva + iva;
        }
        /// <summary>
        /// Formatea un decimal para PayPal (siempre 2 decimales, punto como separador)
        /// </summary>
        public static string FormatearPayPal(decimal valor) =>
            valor.ToString("F2", CultureInfo.InvariantCulture);
    }
}
