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
}
