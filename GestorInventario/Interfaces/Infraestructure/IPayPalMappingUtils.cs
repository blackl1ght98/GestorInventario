using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;

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
