namespace GestorInventario.Interfaces.Application
{
    public interface IPdfService
    {
        Task<(bool, string, byte[])> DescargarPDF();
        Task<(bool, string, byte[])> DescargarProductoPDF();
    }
}
