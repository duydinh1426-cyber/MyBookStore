using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Email { get; set; }

    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
