using GestorInventario.Domain.Models.ViewModels.Paypal.GestorInventario.Domain.Models.ViewModels.Paypal;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.Domain.Models.ViewModels.Paypal
{
    public class PlanesPaginadosViewModel
    {
        public IEnumerable<PlanesViewModel> Planes { get; set; }
        public IEnumerable<PaginasModel> Paginas { get; set; }
        public int PaginaActual { get; set; }
        public bool TienePaginaSiguiente { get; set; }
        public bool TienePaginaAnterior { get; set; }
    }
    public class PlansResponse
    {
        public List<Plan> Plans { get; set; }
        public List<Link> Links { get; set; }
    }

    public class Plan
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string status { get; set; }
        public string usage_type { get; set; }
        public string create_time { get; set; }
        public List<BillingCycle> billing_cycles { get; set; }
        public Taxes taxes { get; set; }
        public List<Link> links { get; set; }
    }

    public class Link
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
        public string encType { get; set; }
    }

}
