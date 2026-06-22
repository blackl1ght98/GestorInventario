namespace GestorInventario.ViewModels
{
    public class DashboardViewModel
    {
        public int PedidosPagados { get; set; }

        public int PedidosDevueltos { get; set; }
        public int PedidosEntregados { get; set; }
        public int PedidosCancelados { get; set; }


        // Nuevas
        public int PedidosHoy { get; set; }
        public int PedidosEsteMes { get; set; }
        public decimal IngresosTotales { get; set; }
        public decimal IngresosMesActual { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal TotalReembolsado { get; set; }
        public double TasaConversion { get; set; } // % pedidos pagados vs total
    }
}
