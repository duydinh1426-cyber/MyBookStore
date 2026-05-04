using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class Admin
{
    public int UserId { get; set; }

    public int AccountId { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public virtual Account Account { get; set; } = null!;
}
