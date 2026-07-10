using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GestorInventario.Shared.Utilities
{
    public static class ConversionExtensions
    {
        private static readonly string[] _dateFormats = new[]
        {
          "dd/MM/yyyy HH:mm:ss",
          "dd/MM/yyyy H:mm:ss",
          "dd/MM/yyyy h:mm:ss tt",
          "yyyy-MM-ddTHH:mm:ssZ",
          "yyyy-MM-ddTHH:mm:ss",
          "yyyy-MM-dd",
          "MM/dd/yyyy",
          "MM-dd-yyyy"
      };

        public static decimal ToDecimalSafe(this object? value)
        {
            if (value is null) return 0m;
            if (value is decimal d) return d;
            if (value is string s && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var ds))
                return ds;
            return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dr) ? dr :
    0m;
        }

        public static int ToIntSafe(this object? value)
        {
            if (value is null) return 0;
            if (value is int i) return i;
            if (value is string s && int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var is2))
                return is2;
            return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ir) ? ir : 0;
        }

        public static DateTime ToDateTimeSafe(this object? value)
        {
            if (value is DateTime dt) return dt;
            var s = value?.ToString()?.Trim('{', '}').Trim();
            if (string.IsNullOrEmpty(s)) return DateTime.UtcNow;
            return DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dr)
                ? dr
                : DateTime.UtcNow;
        }
    }
}
