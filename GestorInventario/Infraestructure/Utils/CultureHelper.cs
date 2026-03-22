using System.Globalization;

namespace GestorInventario.Infraestructure.Utils
{
    public class CultureHelper
    {
        private readonly ILogger<CultureHelper> _logger;

        public CultureHelper(ILogger<CultureHelper> logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Establece la cultura invariante en el hilo actual para operaciones de formato y parsing seguros.
        /// Útil para APIs externas (PayPal, JSON, decimales, fechas) que requieren consistencia global.
        /// </summary>
        public static void SetInvariantCulture()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Versión con logging y try-catch por si quieres más control.
        /// </summary>
        public void SetInvariantCultureSafe()
        {
            try
            {
                SetInvariantCulture();
                _logger?.LogDebug("Cultura invariante establecida en el hilo actual.");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "No se pudo establecer cultura invariante.");
            }
        }
    }
}
