using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;

namespace GestorInventario.Files
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
