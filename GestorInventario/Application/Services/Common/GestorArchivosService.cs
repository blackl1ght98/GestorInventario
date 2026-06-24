using GestorInventario.Interfaces.Application.Common;

namespace GestorInventario.Application.Services.Common
{
    public class GestorArchivosService : IGestorArchivos
    {
        private readonly IWebHostEnvironment _env; // Para poder localizar wwwroot
       

        public GestorArchivosService(IWebHostEnvironment env)
        {
            _env = env;
          
        }

        public Task BorrarArchivo(string ruta, string carpeta)
        {
            if (ruta != null)
            {
                var nombreArchivo = Path.GetFileName(ruta);
                string directorioArchivo = Path.Combine(_env.WebRootPath, carpeta, nombreArchivo);

                if (File.Exists(directorioArchivo))
                {
                    File.Delete(directorioArchivo);
                }
            }

            return Task.FromResult(0);
        }



        public async Task<string> GuardarArchivo(byte[] contenido, string extension, string carpeta)
        {
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";

            string folder = Path.Combine(_env.WebRootPath, carpeta);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string rutaFisica = Path.Combine(folder, nombreArchivo);

            await File.WriteAllBytesAsync(rutaFisica, contenido);

            
            return Path.Combine(carpeta, nombreArchivo).Replace("\\", "/");
        }
    }
}
