using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.Files;

namespace GestorInventario.Application.Services.Files
{
    public class BarCodeImageStorage : IBarCodeImageStorage
    {
        private readonly IGestorArchivos _gestor;

        public BarCodeImageStorage(IGestorArchivos gestor)
        {
            _gestor = gestor;
        }
        public async Task<string> SaveAsync(byte[] bytes, string extension, string folder)
       => await _gestor.GuardarArchivo(bytes, extension, folder);
    }
}
