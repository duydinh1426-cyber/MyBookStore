using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class Customer
{
    public int UserId { get; set; }

    public int AccountId { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
