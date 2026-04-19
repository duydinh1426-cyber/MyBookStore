using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Order?> GetOrderByIdAsync(int orderId);
        void AddPayment(Payment payment);
        Task<bool> SaveChangesAsync();
    }
}