using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class UserSubscription
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? NombreSusbcriptor { get; set; }

    public string SubscriptionId { get; set; } = null!;

    public string? PaypalPlanId { get; set; }

    public virtual SubscriptionDetail Subscription { get; set; } = null!;

    public virtual Usuario? User { get; set; }
}
