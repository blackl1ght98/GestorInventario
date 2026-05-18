namespace GestorInventario.Interfaces.Application.Common
{
    public interface IConversionUtils
    {
        decimal? ConvertToDecimal(object value);
        int? ConvertToInt(object value);
        DateTime? ConvertToDateTime(object value);
    }
}
