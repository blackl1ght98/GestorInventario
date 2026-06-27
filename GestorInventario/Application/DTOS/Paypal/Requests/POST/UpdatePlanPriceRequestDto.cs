using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOS.Paypal.Requests.POST
{
    public record UpdatePlanPriceRequestDto
    {
        [Required(ErrorMessage = "El ID del plan es requerido.")]
        public required string PlanId { get; init; }

        [Required(ErrorMessage = "El precio regular es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio regular debe ser mayor a 0.")]
        public decimal RegularAmount { get; init; }

        [Range(0.00, double.MaxValue, ErrorMessage = "El precio de prueba debe ser 0 o mayor.")]
        public decimal? TrialAmount { get; init; }

        [Required(ErrorMessage = "La moneda es requerida.")]
        public required string Currency { get; init; } 
    }
}
