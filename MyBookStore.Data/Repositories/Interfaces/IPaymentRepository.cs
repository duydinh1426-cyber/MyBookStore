using MyBookStore.Data.Models;

namespace Data.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        void AddPayment(Payment payment);
        Task<bool> SaveChangesAsync();
    }
}