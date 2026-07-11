
using System.Globalization;

namespace GestorInventario.Shared.Utilities
{
    public class CultureHelper
    {
       

        /// <summary>
        /// Establece la cultura invariante en el hilo actual para operaciones de formato y parsing seguros.
        /// Útil para APIs externas (PayPal, JSON, decimales, fechas) que requieren consistencia global.
        /// </summary>
        public static void SetInvariantCulture()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

       
    }
}
