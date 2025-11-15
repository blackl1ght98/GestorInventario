namespace GestorInventario.Infraestructure.Utils
{
    public record OperationResult<T>(
        bool Success,
        string Message,
        T? Data = default
    )
    {
        public static OperationResult<T> Ok(string message = "Operación exitosa", T? data = default) =>
            new(true, message, data);

        public static OperationResult<T> Fail(string message) =>
            new(false, message);
    }
}
