using System;
using System.Collections.Generic;
using System.Text;

namespace GestorInventario.Shared.DTOS.Configuration
{
    public class PaypalSettings
    {
        public const string SectionName = "Paypal";
        public ReturnUrlSet ReturnUrls { get; set; } = new();
        public CancelUrlSet CancelUrls { get; set; } = new();
    }
    public class ReturnUrlSet
    {
        public string Development { get; set; } = string.Empty;
        public string Docker { get; set; } = string.Empty;
    }
    public class CancelUrlSet
    {
        public string Development { get; set; } = string.Empty;
        public string Docker { get; set; } = string.Empty;
    }
}
