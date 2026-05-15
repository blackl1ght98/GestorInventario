using GestorInventario.Interfaces;

namespace GestorInventario.Application.Services
{
    public class GestorArchivosService : IGestorArchivos
    {
        private readonly IWebHostEnvironment env; // Para poder localizar wwwroot
        private readonly IHttpContextAccessor httpContextAccessor; // Para conocer la configuración del servidor para construir la url de la imagen

        public GestorArchivosService(IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor)
        {
            this.env = env;
            this.httpContextAccessor = httpContextAccessor;
        }

        public Task BorrarArchivo(string ruta, string carpeta)
        {
            if (ruta != null)
            {
                var nombreArchivo = Path.GetFileName(ruta);
                string directorioArchivo = Path.Combine(env.WebRootPath, carpeta, nombreArchivo);

                if (File.Exists(directorioArchivo))
                {
                    File.Delete(directorioArchivo);
                }
            }

            return Task.FromResult(0);
        }

        public async Task<string> EditarArchivo(byte[] contenido, string extension, string carpeta, string ruta
            )
        {
            await BorrarArchivo(ruta, carpeta);
            return await GuardarArchivo(contenido, extension, carpeta);
        }

        public async Task<string> GuardarArchivo(byte[] contenido, string extension, string carpeta)
        {
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";

            string folder = Path.Combine(env.WebRootPath, carpeta);

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
