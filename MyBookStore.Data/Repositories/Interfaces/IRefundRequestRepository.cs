using MyBookStore.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IRefundRequestRepository
    {
        void AddRefundRequest(RefundRequest r);
        Task<List<RefundRequest>> GetRefundRequestsAsync(string? status); // lấy tất cả yêu cầu hoàn tiền
        Task<RefundRequest?> GetRefundRequestByIdAsync(int id); // lấy yêu cầu hoàn tiền theo id
    }
}
