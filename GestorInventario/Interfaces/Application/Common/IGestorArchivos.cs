namespace GestorInventario.Interfaces.Application.Common
{
    public interface IGestorArchivos
    {
      
        Task BorrarArchivo(string ruta, string carpeta);
        Task<string> GuardarArchivo(byte[] contenido, string extension, string carpeta);
    }
}
