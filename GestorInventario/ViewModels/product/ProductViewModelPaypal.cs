using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.product
{
   
    
    public class ProductViewModelPaypal
    {
        [Required(ErrorMessage ="El nombre del producto es requerido")]
        public  string? Name { get; set; }
        [Required(ErrorMessage ="La descripcion del producto es requerida")]
        public string? Description { get; set; }
        [Required(ErrorMessage ="El tipo de producto es requerido")]
        public string? Type { get; set; }
        [Required(ErrorMessage ="La categoria del producto es requerida")]
        public string? Category { get; set; }
        [Required(ErrorMessage ="El nombre del plan es requerido")]
        public string? PlanName { get; set; }
        [Required(ErrorMessage ="La descripcion del plan es requerida")]
        public string? PlanDescription { get; set; }
        [Required(ErrorMessage ="El precio es requerido")]
        public decimal Amount { get; set; }
        public string IntervaUnit { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        // Propiedades para el periodo de prueba
        public bool HasTrialPeriod { get; set; } = false;
        public int TrialPeriodDays { get; set; } = 7;  // 7 días por defecto
        public decimal TrialAmount { get; set; } = 0.00m;  // Gratis por defecto
    }
   



}
