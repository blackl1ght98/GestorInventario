namespace GestorInventario.Shared.Utilities
{
    /// <summary>
    /// Clase central de resultados de operación. 292 referencias en el proyecto.
    /// 
    /// ⚠️ ADVERTENCIA: No modificar la estructura de esta clase sin revisar TODAS las referencias.
    /// Cambios en los constructores, propiedades o métodos estáticos pueden romper
    /// múltiples capas de la aplicación simultáneamente.
    /// 
    /// El constructor sin parámetros es necesario para que PolicyHandler pueda
    /// crear instancias mediante Activator.CreateInstance en la acción de fallback.
    /// No eliminar aunque parezca innecesario.
    ///
    /// Si necesitas añadir funcionalidad, hazlo de forma aditiva (nuevos métodos)
    /// sin modificar los existentes.
    /// </summary>
    public record OperationResult<T>(bool Success, string Message, T? Data = default)
    {
        public OperationResult() : this(false, string.Empty, default) { }
        public bool IsSuccess => Success;
        public bool IsFailure => !Success;
        public bool HasData => Data is not null;
     
        public static OperationResult<T> Ok(string message = "Operación exitosa", T? data = default)
            => new(true, message, data);

        public static OperationResult<T> Fail(string message, T? data = default)
            => new(false, message, data);

       
        public static OperationResult<T> Fail(string message)
            => Fail(message, default);
    }
}
