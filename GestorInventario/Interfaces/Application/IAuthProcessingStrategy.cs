namespace GestorInventario.Interfaces.Application
{
    // Capa 3: Interfaz - Contrato común
    public interface IAuthProcessingStrategy
    {
        // Define QUÉ debe hacer cada estrategia, no CÓMO lo hace
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
