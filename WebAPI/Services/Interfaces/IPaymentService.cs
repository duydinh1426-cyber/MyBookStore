namespace WebAPI.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Dictionary<string, object?>> CreateVnPayUrlAsync(int userId, int orderId, HttpContext context);
        Task<Dictionary<string, object?>> HandleCallbackAsync(IQueryCollection query);
    }
}