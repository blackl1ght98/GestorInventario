namespace GestorInventario.Interfaces.Application
{
    public interface IPdfService
    {
        Task<(bool, string, byte[])> GenerarReporteHistorialPedidosAsync();
        Task<(bool, string, byte[])> DescargarProductoPDF();
    }
}
