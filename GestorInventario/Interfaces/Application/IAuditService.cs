namespace GestorInventario.Interfaces.Application
{
    public interface IAuditService
    {
        Task RegistrarAuditoriaAsync(
        string tabla,
        string operacion,
        int registroId,
        int? usuarioId = null,
        string? campo = null,
        string? valorAnterior = null,
        string? valorNuevo = null,
        string? descripcion = null);
    }
}
