using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int UserId { get; set; }

    public int BookId { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Customer User { get; set; } = null!;
}
