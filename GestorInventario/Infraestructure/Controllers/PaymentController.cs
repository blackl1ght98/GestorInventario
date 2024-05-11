using GestorInventario.Application.Services;
using GestorInventario.Domain.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentController(ILogger<PaymentController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Success(string paymentId, string token, string PayerId) 
        {
            ViewData["PaymentId"]= paymentId;
            ViewData["token"]= token;
            ViewData["payerId"]= PayerId;
            return View();
        
        }
    //    [HttpPost]
    //    public async Task<IActionResult> PayUsingCard(PaymentViewModel model)
    //    {
    //        // Cambia la cultura actual del hilo a InvariantCulture
    //        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

    //        // Cambia la cultura de la interfaz de usuario actual del hilo a InvariantCulture
    //        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

    //        try
    //        {
    //            if (model.amount == 0)
    //            {
    //                TempData["error"] = "please enter amount";
    //                return RedirectToAction(nameof(Index));
    //            }

    //            string amountString = model.amount.ToString().Replace(",", ".");
    //            decimal amount = Decimal.Parse(amountString, CultureInfo.InvariantCulture);
    //            string returnUrl = "https://localhost:7056/Payment/Success";
    //            string cancelUrl = "https://localhost:7056/Payment/Cancel";
    //            var createdPayment = await _unitOfWork.PaypalService.CreateOrderAsync(amount, returnUrl, cancelUrl, model.currency);
    //            string approvalUrl = createdPayment.links.FirstOrDefault(x => x.rel.ToLower() == "approval_url")?.href;
    //            if (!string.IsNullOrEmpty(approvalUrl))
    //            {
    //                return Redirect(approvalUrl);
    //            }
    //            else
    //            {
    //                TempData["error"] = "Fallo al iniciar el pago con paypal";
    //            }
    //        }
    //        catch (Exception ex)
    //        {

    //            TempData["error"] = ex.Message; 
                
    //        }
    //        return RedirectToAction(nameof(Index));
    //    }
    }
}
