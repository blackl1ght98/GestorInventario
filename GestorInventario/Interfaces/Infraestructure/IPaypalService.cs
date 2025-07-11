﻿using GestorInventario.Application.Classes;
using GestorInventario.Application.DTOs;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaypalService
    {
      
       
      
        Task<string> GetAccessTokenAsync();
        //Hacer pedido   
        Task<string> CreateOrderAsyncV2(Checkout pagar);
        //Obtener detaller de un pedido     
        Task<dynamic> ObtenerDetallesPagoEjecutadoV2(string id);
        //Hacer el reembolso
        Task<string> RefundSaleAsync(int pedidoId, string currency);
        Task<string> CreateSubscriptionPlanAsync(string productId, string planName, string description, decimal amount, string currency, int trialDays = 0, decimal trialAmount = 0.00m);
        Task<HttpResponseMessage> CreateProductAsync(string productName, string productDescription, string productType, string productCategory);
        Task<string> DesactivarPlan(string productId, string planId);
        Task<string> MarcarDesactivadoProducto(string id);
        Task<string> EditarProducto(string id, string name, string description);
        Task<string> Subscribirse(string id, string returnUrl, string cancelUrl, string planName);
        Task<PaypalSubscriptionResponse> ObtenerDetallesSuscripcion(string subscription_id);
   
        Task<PaypalPlanResponse> ObtenerDetallesPlan(string id);
        Task<(string ProductsResponse, bool HasNextPage)> GetProductsAsync(int page = 1, int pageSize = 10);
        Task<string> CancelarSuscripcion(string subscription_id, string reason);
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret);
       
        Task<(List<dynamic> plans, bool HasNextPage)> GetSubscriptionPlansAsyncV2(int page = 1, int pageSize = 6);
        Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory);


    }
}
