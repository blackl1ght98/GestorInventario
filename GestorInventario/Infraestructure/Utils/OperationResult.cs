namespace GestorInventario.Infraestructure.Utils
{
    public record OperationResult<T>(bool Success, string Message, T? Data = default)
    {
        public bool IsSuccess => Success;
        public bool IsFailure => !Success;
        public bool HasData => Data is not null;

        public static OperationResult<T> Ok(string message = "Operación exitosa", T? data = default)
            => new(true, message, data);

        public static OperationResult<T> Fail(string message, T? data = default)
            => new(false, message, data);

        // Mantienes la sobrecarga antigua para 0 impacto
        public static OperationResult<T> Fail(string message)
            => Fail(message, default);
    }
}
