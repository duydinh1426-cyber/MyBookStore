using WebAPI.Enums;

namespace WebAPI.DTOs
{
    public record CheckoutDto(string Phone, string Address, string? Note, string PaymentMethod = "cod", decimal ShippingFee = 0);
    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public OrderStatus GetStatus() => Status.ToEnum();
    }

    public class CancelOrderDto
    {
        public string? Note { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankName { get; set; }
    }

    public class ResolveRefundDto
    {
        public string? AdminNote { get; set; }
    }

    // DTOs/SePayWebhookDto.cs
    public class SePayWebhookDto
    {
        public string? Content { get; set; }        // Nội dung CK
        public decimal TransferAmount { get; set; } // Số tiền
        public string? TransferType { get; set; }   // "in" = tiền vào
        public string? ReferenceCode { get; set; }
    }

    public class OrderResponseDto
    {
        public int orderId { get; set; }
        public decimal totalCost { get; set; }
        public int itemCount { get; set; }
        public string? paymentMethod { get; set; }
        public bool requiresPayment { get; set; }
    }
}