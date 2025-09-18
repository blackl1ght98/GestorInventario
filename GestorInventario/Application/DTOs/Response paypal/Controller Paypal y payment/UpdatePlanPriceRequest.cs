using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs.Response_paypal.Controller_Paypal_y_payment
{
    public class UpdatePlanPriceRequest
    {
        [Required(ErrorMessage = "El ID del plan es requerido.")]
        public required string PlanId { get; set; }

        [Required(ErrorMessage = "El precio regular es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio regular debe ser mayor a 0.")]
        public decimal RegularAmount { get; set; }

        [Range(0.00, double.MaxValue, ErrorMessage = "El precio de prueba debe ser 0 o mayor.")]
        public decimal? TrialAmount { get; set; }

        [Required(ErrorMessage = "La moneda es requerida.")]
        public required string Currency { get; set; } 
    }
}
