namespace GestorInventario.Shared.Utilities;

public static class OperationResultExtensions
{
    // ESTE es el que te permite hacer: await _service.MetodoAsync().BindAsync(...)
    public static async Task<OperationResult<TOut>> BindAsync<T, TOut>(
        this Task<OperationResult<T>> task,
        Func<T, Task<OperationResult<TOut>>> next)
    {
        var result = await task;
        if (!result.Success) return OperationResult<TOut>.Fail(result.Message);
        return await next(result.Data!);
    }

    // ESTE es el que te permite hacer: await _service.MetodoAsync().Bind(...)
    public static async Task<OperationResult<TOut>> Bind<T, TOut>(
        this Task<OperationResult<T>> task,
        Func<T, OperationResult<TOut>> next)
    {
        var result = await task;
        if (!result.Success) return OperationResult<TOut>.Fail(result.Message);
        return next(result.Data!);
    }
    // ESTE es para transformar el dato interno sin cambiar el estado de éxito/fallo
    public static async Task<OperationResult<TOut>> Map<T, TOut>(
        this Task<OperationResult<T>> task,
        Func<T, TOut> mapper)
    {
        var result = await task;
        if (!result.Success) return OperationResult<TOut>.Fail(result.Message);
        return OperationResult<TOut>.Ok(mapper(result.Data!));
    }
    /// <summary>
    /// Ejecuta una acción con el dato si la operación fue exitosa,
    /// pero mantiene el mismo resultado original.
    /// </summary>
    public static async Task<OperationResult<T>> Tap<T>(
        this Task<OperationResult<T>> task,
        Func<T, Task> action)
    {
        var result = await task;
        if (result.Success && result.Data != null)
        {
            await action(result.Data);
        }
        return result;
    }

// Versión síncrona por si la acción no es async
    public static async Task<OperationResult<T>> Tap<T>(
        this Task<OperationResult<T>> task,
        Action<T> action)
    {
        var result = await task;
        if (result.Success && result.Data != null)
        {
            action(result.Data);
        }
        return result;
    }
    /// <summary>
    /// Convierte un valor simple en un OperationResult basado en una función de validación.
    /// Útil para iniciar cadenas de BindAsync desde métodos que devuelven null o valores simples.
    /// </summary>
    public static OperationResult<T> ToOperationResult<T>(
        this T? value,
        Func<T?, OperationResult<T>> validator)
    {
        return validator(value);
    }
}
