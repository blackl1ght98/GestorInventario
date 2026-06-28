namespace GestorInventario.Application.Exceptions
{
    public class PayPalException : Exception
    {
        public PayPalException(string message) : base(message) { }
    }
}
