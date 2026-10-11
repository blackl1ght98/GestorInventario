namespace GestorInventario.Shared.Utilities
{
    /// <summary>
    /// Clase central de resultados de operación. 478 referencias en el proyecto.
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
    /// <typeparam name="T">Tipo de dato que contiene el resultado de la operación.</typeparam>
    public record OperationResult<T>(bool Success, string Message, T? Data = default)
    {
        /// <summary>
        /// Constructor vacío requerido para instanciación dinámica mediante reflexión (Activator).
        /// Inicializa el resultado como fallido por defecto.
        /// </summary>
        public OperationResult() : this(false, string.Empty, default) { }

        /// <summary> Indica si la operación se completó exitosamente. </summary>
        public bool IsSuccess => Success;

        /// <summary> Indica si la operación falló. </summary>
        public bool IsFailure => !Success;

        /// <summary> Verifica si la propiedad Data contiene un valor no nulo. </summary>
        public bool HasData => Data is not null;

        #region Métodos de Creación Exitosa (Ok)

        /// <summary> Crea un resultado exitoso con un mensaje personalizado y datos opcionales. </summary>
        public static OperationResult<T> Ok(T? data, string message)
            => new(true, message, data);

        /// <summary> Crea un resultado exitoso centrado en la devolución de datos. </summary>
        public static OperationResult<T> Ok(T? data = default)
            => new(true, "Operación exitosa", data);

        /// <summary> Crea un resultado exitoso devolviendo solo un mensaje de confirmación. </summary>
        public static OperationResult<T> Ok(string message)
            => new(true, message, default);

        /// <summary> Crea un resultado exitoso genérico sin datos adicionales. </summary>
        public static OperationResult<T> Ok()
            => new(true, "Operación exitosa", default);

        #endregion

        #region Métodos de Creación Fallida (Fail)

        /// <summary> Crea un resultado fallido con un mensaje de error específico. </summary>
        public static OperationResult<T> Fail(string message)
            => new(false, message, default);

        /// <summary> Crea un resultado fallido con un mensaje de error genérico. </summary>
        public static OperationResult<T> Fail()
            => new(false, "Operación fallida", default);

        #endregion

        /// <summary>
        /// Extrae el valor de <see cref="Data"/> asegurando que la operación fue exitosa y que el dato no es nulo.
        /// </summary>
        /// <returns>El valor de tipo <typeparamref name="T"/> contenido en Data.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si <see cref="Success"/> es falso o si <see cref="Data"/> es nulo.</exception>
        /// <remarks>
        /// Use este método cuando esté seguro de que el flujo de negocio garantiza la existencia de Data
        /// si la operación fue exitosa. Evita comprobaciones manuales de nulos repetitivas.
        /// </remarks>
        public T Unwrap()
        {
            if (!Success || Data == null)
                throw new InvalidOperationException($"Intentando acceder a Data en un resultado fallido o vacío: {Message}");
            return Data;
        }
        /// <summary> Transforma el dato interno a otro tipo si la operación fue exitosa. </summary>
        public OperationResult<TOut> Map<TOut>(Func<T, TOut> mapper)
        {
            if (!Success) return OperationResult<TOut>.Fail(Message);
            try
            {
                // El orden correcto es: (Mensaje, Dato)
                return OperationResult<TOut>.Ok( mapper(Data!),Message);
            }
            catch (Exception ex)
            {
                return OperationResult<TOut>.Fail($"Error en transformación: {ex.Message}");
            }
        }

        /// <summary> Encadena otra operación asíncrona que devuelve un OperationResult. </summary>
        public async Task<OperationResult<TOut>> BindAsync<TOut>(Func<T, Task<OperationResult<TOut>>> next)
        {
            if (!Success) return OperationResult<TOut>.Fail(Message);
            return await next(Data!);
        }

        /// <summary> Valida que el dato cumpla una condición; si no, marca el resultado como fallido. </summary>
        public OperationResult<T> Ensure(Func<T, bool> predicate, string errorMessage)
        {
            if (!Success) return this;
            return predicate(Data!) ? this : OperationResult<T>.Fail(errorMessage);
        }

    }
}