namespace GestorInventario.Interfaces.Utils
{
    public interface IConversionUtils
    {
        decimal? ConvertToDecimal(object value);
        int? ConvertToInt(object value);
    }
}
