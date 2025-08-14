namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {
        decimal? ConvertToDecimal(object value);
        int? ConvertToInt(object value);
        DateTime? ConvertToDateTime(object value);

    }
}
