using GestorInventario.Domain.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestorInventario.Domain.Models.ViewModels.Paypal;
using GestorInventario.Application.Services;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;

        public PaymentController(ILogger<PaymentController> logger, IUnitOfWork unitOfWork, IConfiguration configuration, GestorInventarioContext context)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _context = context;
            
        }


       
        [AllowAnonymous]
        public async Task<IActionResult> Success(string paymentId, string PayerID)
        {
            try
            {
                var requestData = new { payer_id = PayerID };

                using (var httpClient = new HttpClient())
                {
                    var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                    var clientSecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
                    var authToken = await _unitOfWork.PaypalService.GetAccessTokenAsync(clientId, clientSecret);

                    if (string.IsNullOrEmpty(authToken))
                        throw new Exception("No se pudo obtener el token de autenticación.");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync($"https://api-m.sandbox.paypal.com/v1/payments/payment/{paymentId}/execute", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Error al ejecutar el pago: {response.StatusCode} - {responseBody}");
                    }

                    // **Parseo manual de la respuesta JSON sin usar Payment de la API de PayPal**
                    var jsonResponse = JObject.Parse(responseBody);
                    var saleId = jsonResponse["transactions"]?[0]?["related_resources"]?[0]?["sale"]?["id"]?.ToString();
                    var total = jsonResponse["transactions"]?[0]?["related_resources"]?[0]?["sale"]?["amount"]?["total"]?.ToString();
                    var currency = jsonResponse["transactions"]?[0]?["related_resources"]?[0]?["sale"]?["amount"]?["currency"]?.ToString();

                    if (string.IsNullOrEmpty(saleId) || string.IsNullOrEmpty(total) || string.IsNullOrEmpty(currency))
                    {
                        throw new Exception("No se pudo extraer la información del pago.");
                    }

                    // **Actualizar la base de datos manualmente**
                    var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(existeUsuario, out int usuarioId))
                    {
                        var pedido = await _context.Pedidos
                            .Where(p => p.IdUsuario == usuarioId && p.EstadoPedido == "En Proceso")
                            .OrderByDescending(p => p.FechaPedido)
                            .FirstOrDefaultAsync();

                        if (pedido != null)
                        {
                            pedido.SaleId = saleId;
                            pedido.Total = total;
                            pedido.Currency = currency;
                            pedido.PagoId = paymentId;
                            pedido.EstadoPedido = "Pagado";
                            _context.Update(pedido);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }


        //Si el pago es rechazado viene aqui
        [HttpPost]
        public async Task<IActionResult> RefundSale([FromBody] RefundRequestModel request)
        {
            if (request == null || request.PedidoId <= 0)
            {
                return BadRequest("Solicitud inválida.");
            }

            try
            {
                
               // var refund = await _unitOfWork.PaypalService.RefundSaleAsync(request.PedidoId,  request.currency);
                var refund = await _unitOfWork.PaypalService.RefundSaleAsync(request.PedidoId, request.currency);
                return Ok(refund);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar el reembolso: {ex.Message}");
            }
        }
      

       
    }
}
