using WebAPI.Enums;

namespace WebAPI.DTOs
{
    public record CheckoutDto(string Phone, string Address, string? Note);
    public record UpdateOrderStatusDto(OrderStatus Status);
}
