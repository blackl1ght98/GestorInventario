using GestorInventario.Application.DTOS;
using GestorInventario.Infrastructure.ExternalServices;

namespace GestorInventario.Infrastructure.Mappers;

public static class SubscriptionDetailsMapper
{
    public static PaypalSubscriptionDto ToDto(PaypalSubscriptionResponse response)
    {
        return new PaypalSubscriptionDto
        {
            Id = response.Id,
            PlanId = response.PlanId,
            Status = response.Status,
            StartTime = response.StartTime,
            StatusUpdateTime = response.StatusUpdateTime,
            Subscriber = new SubscriberDto
            {
                EmailAddress = response.Subscriber.EmailAddress,
                Name = new NameDto
                {
                    GivenName = response.Subscriber.Name.GivenName,
                    Surname = response.Subscriber.Name.Surname
                },
                PayerId = response.Subscriber.PayerId,

            },
            BillingInfo = new BillingInfoDto
            {
                OutstandingBalance = new AmountDto
                {
                    Value = response.BillingInfo.OutstandingBalance.Value,
                    CurrencyCode = response.BillingInfo.OutstandingBalance.CurrencyCode

                },
                NextBillingTime = response.BillingInfo.NextBillingTime,
                LastPayment = new LastPaymentDto
                {
                    Amount = new AmountDto
                    {
                        Value = response.BillingInfo.LastPayment.Amount.Value,
                        CurrencyCode = response.BillingInfo.LastPayment.Amount.CurrencyCode
                    }
                },
                FinalPaymentTime = response.BillingInfo.FinalPaymentTime,
                CycleExecutions = response.BillingInfo.CycleExecutions.Select(c => new CycleExecutionDto
                {
                    CyclesCompleted = c.CyclesCompleted,
                    CyclesRemaining = c.CyclesRemaining,
                    TotalCycles = c.TotalCycles
                }).ToList() ?? new List<CycleExecutionDto>()
            }

        };
    }
}