using GestorInventario.Application.DTOs;
using GestorInventario.Interfaces.Infraestructure;

namespace GestorInventario.Infraestructure.Utils
{
    public class PayPalMappingUtils: IPayPalMappingUtils
    {
        public List<BillingCycle> MapBillingCycles(List<BillingCycle> billingCycles)
        {
            if (billingCycles == null)
                return new List<BillingCycle>();

            // Si los tipos coinciden exactamente, solo devolvemos o clonamos (si quieres evitar referencias directas)
            return billingCycles.Select(cycle => new BillingCycle
            {
                TenureType = cycle.TenureType,
                Sequence = cycle.Sequence,
                TotalCycles = cycle.TotalCycles,
                Frequency = MapFrequency(cycle.Frequency),
                PricingScheme = MapPricingScheme(cycle.PricingScheme)
            }).ToList();
        }

        public Frequency MapFrequency(Frequency frequency)
        {
            if (frequency == null)
                return null;

            return new Frequency
            {
                IntervalUnit = frequency.IntervalUnit,
                IntervalCount = frequency.IntervalCount
            };
        }
        public PricingScheme MapPricingScheme(PricingScheme pricingScheme)
        {
            if (pricingScheme == null)
                return null;

            return new PricingScheme
            {
                Version = pricingScheme.Version,
                FixedPrice = MapMoney(pricingScheme.FixedPrice),
                CreateTime = pricingScheme.CreateTime,
                UpdateTime = pricingScheme.UpdateTime
            };
        }

        public Money MapMoney(Money money)
        {
            if (money == null)
                return null;

            return new Money
            {
                CurrencyCode = money.CurrencyCode,
                Value = money.Value
            };
        }

        public Taxes MapTaxes(Taxes taxes)
        {
            if (taxes == null)
                return null;

            return new Taxes
            {
                Percentage = taxes.Percentage,
                Inclusive = taxes.Inclusive
            };
        }
    }
}
