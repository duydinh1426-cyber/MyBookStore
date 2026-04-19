using WebAPI.Enums;

namespace WebAPI.DTOs
{
    public record CheckoutDto(string Phone, string Address, string? Note, string PaymentMethod = "cod");
    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public OrderStatus GetStatus() => Status.ToEnum();
    }
}
