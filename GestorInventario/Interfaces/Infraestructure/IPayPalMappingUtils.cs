using GestorInventario.Application.DTOs;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPayPalMappingUtils
    {
        List<BillingCycle> MapBillingCycles(List<BillingCycle> billingCycles);
        Frequency MapFrequency(Frequency frequency);
        PricingScheme MapPricingScheme(PricingScheme pricingScheme);
        Money MapMoney(Money money);
        Taxes MapTaxes(Taxes taxes);
    }
}
