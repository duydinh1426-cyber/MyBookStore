using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class RefundRequest
{
    public int RefundRequestId { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = null!;

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string BankAccountNumber { get; set; } = null!;

    public string BankAccountName { get; set; } = null!;

    public string BankName { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Customer User { get; set; } = null!;
}
