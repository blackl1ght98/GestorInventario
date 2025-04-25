namespace GestorInventario.Domain.Models.ViewModels.Paypal
{
    public class ProductoViewModel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }

    }
    
    public class ProductViewModelPaypal
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string PlanName { get; set; }
        public string PlanDescription { get; set; }
        public decimal Amount { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
        // Propiedades para el periodo de prueba
        public bool HasTrialPeriod { get; set; } = false;
        public int TrialPeriodDays { get; set; } = 7;  // 7 días por defecto
        public decimal TrialAmount { get; set; } = 0.00m;  // Gratis por defecto
    }
   



}
