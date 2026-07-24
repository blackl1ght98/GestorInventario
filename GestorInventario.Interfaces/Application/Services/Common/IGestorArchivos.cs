namespace GestorInventario.Interfaces.Application.Services.Common
{
    public interface IGestorArchivos
    {
      
        Task BorrarArchivo(string ruta, string carpeta);
        Task<string> GuardarArchivo(byte[] contenido, string extension, string carpeta);
    }
}
