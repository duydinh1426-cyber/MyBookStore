namespace WebAPI.DTOs
{
    public record CartItemResponseDto(
        int CartItemId,
        int BookId,
        string Title,
        string Author,
        string? Image,
        decimal Price,
        int Quantity,
        decimal SubTotal
    );

    public record CartResponseDto(
            List<CartItemResponseDto> Items,
            decimal TotalPrice,
            int TotalItems
    );
    
    public record AddCartDto(
        int BookId,
        int Quantity
    );
    
    public record UpdateCartDto(
        int Quantity
    );
}
