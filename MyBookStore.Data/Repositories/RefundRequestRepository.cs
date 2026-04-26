using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories
{
    public class RefundRequestRepository : IRefundRequestRepository
    {
        private readonly DBContext _db;

        public RefundRequestRepository(DBContext db) => _db = db;

        public void AddRefundRequest(RefundRequest r) => _db.RefundRequests.Add(r);

        public async Task<List<RefundRequest>> GetRefundRequestsAsync(string? status)
        {
            var query = _db.RefundRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            query = query.OrderByDescending(r => r.CreatedAt);

            return await query.ToListAsync();
        }

        public async Task<RefundRequest?> GetRefundRequestByIdAsync(int id)
        {
            return await _db.RefundRequests
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RefundRequestId == id);
        }
    }
}
