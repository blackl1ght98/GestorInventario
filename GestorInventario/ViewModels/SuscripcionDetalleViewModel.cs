namespace GestorInventario.ViewModels
{
    public class SuscripcionDetalleViewModel
    {
        // Datos básicos
        public string SubscriptionId { get; set; }
        public string PlanId { get; set; }
        public string Status { get; set; }
        public string SubscriberName { get; set; }
        public string SubscriberEmail { get; set; }
        public string PayerId { get; set; }

        // Fechas ya calculadas
        public DateTime StartDate { get; set; }
        public DateTime StatusUpdateDate { get; set; }
        public DateTime? NextBillingTime { get; set; }
        public DateTime? LastPaymentTime { get; set; }
        public DateTime FinalPaymentTime { get; set; }

        // Facturación
        public decimal? OutstandingBalance { get; set; }
        public string? OutstandingCurrency { get; set; }
        public decimal? LastPaymentAmount { get; set; }
        public string? LastPaymentCurrency { get; set; }

        // Ciclos ya calculados
        public int TrialDays { get; set; }
        public int? CyclesCompleted { get; set; }
        public int? CyclesRemaining { get; set; }
        public int? TotalCycles { get; set; }
        public bool MostrarCiclos { get; set; }
        public bool EnPeriodoPrueba { get; set; }
        public bool MostrarPeriodoPrueba { get; set; }
    }
}
