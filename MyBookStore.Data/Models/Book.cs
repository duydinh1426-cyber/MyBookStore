using System;
using System.Collections.Generic;

namespace MyBookStore.Data.Models;

public partial class Book
{
    public int BookId { get; set; }

    public int? CategoryId { get; set; }

    public string? Author { get; set; }

    public string Title { get; set; } = null!;

    public int? PublisherYear { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public decimal Price { get; set; }

    public int? NumberPage { get; set; }

    public int NumberStock { get; set; }

    public int NumberSold { get; set; }

    public decimal AvgRating { get; set; }

    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
