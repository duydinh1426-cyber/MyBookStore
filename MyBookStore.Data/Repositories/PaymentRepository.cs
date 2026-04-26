using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;

namespace Data.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly DBContext _db;
        public PaymentRepository(DBContext db) => _db = db;

        public void AddPayment(Payment payment) => _db.Payments.Add(payment);

        public async Task<bool> SaveChangesAsync() => await _db.SaveChangesAsync() > 0;
    }
}