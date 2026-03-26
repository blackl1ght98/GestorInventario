using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.enums;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalOrderTrackingService
    {
      
    
   
      
        Task<string> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode);
     

    }
}
