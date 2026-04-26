namespace WebAPI.Services.Payments
{
    public interface IPaymentService
    {
        Task<Dictionary<string, object?>> CreateVnPayUrlAsync(int userId, int orderId, HttpContext context);
        Task<Dictionary<string, object?>> HandleCallbackAsync(IQueryCollection query);
        Task<bool> ConfirmQrPaymentAsync(int orderId, decimal amount);
    }
}