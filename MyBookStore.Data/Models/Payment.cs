using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string TransactionId { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? VnPayResponseCode { get; set; }

    public bool Success { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
