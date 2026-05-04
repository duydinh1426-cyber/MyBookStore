namespace WebAPI.Services.Payments
{
    public interface IPaymentService
    {
        Task<ServiceResult<object>> CreateVnPayUrlAsync(int userId, int orderId, HttpContext context);
        Task<ServiceResult<object>> HandleCallbackAsync(IQueryCollection query);
        Task<ServiceResult> ConfirmQrPaymentAsync(int orderId, decimal amount);
    }
}