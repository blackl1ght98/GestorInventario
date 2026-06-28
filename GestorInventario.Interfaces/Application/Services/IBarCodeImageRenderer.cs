using GestorInventario.enums.Productos;
using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IBarCodeImageRenderer
    {
        Task<byte[]> RenderAsync(string barcode, BarcodeType type);
    }
}
