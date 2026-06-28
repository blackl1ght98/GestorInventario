using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IBarCodeImageStorage
    {
        Task<string> SaveAsync(byte[] bytes, string extension, string folder);
    }
}
