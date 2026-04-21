using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal TotalCost { get; set; }

    public string? Note { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? VnpayTxnRef { get; set; }

    public DateTime? PaidAt { get; set; }

    public bool IsPaid { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();

    public virtual Customer User { get; set; } = null!;
}
