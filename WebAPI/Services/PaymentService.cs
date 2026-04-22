using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Data.Vnpay;
using WebAPI.Services.Interfaces;
using WebAPI.Services.VnPay;

namespace WebAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;
        private readonly IVnPayService _vnPay;

        public PaymentService(IPaymentRepository repo, IVnPayService vnPay)
        {
            _repo = repo;
            _vnPay = vnPay;
        }

        public async Task<Dictionary<string, object?>> CreateVnPayUrlAsync(int userId, int orderId, HttpContext context)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null) return new Dictionary<string, object?> { ["message"] = "NotFound" };
            if (order.UserId != userId) return new Dictionary<string, object?> { ["message"] = "Forbidden" };
            if (order.IsPaid) return new Dictionary<string, object?> { ["message"] = "Đơn hàng này đã được thanh toán." };
            if (order.Status == "cancelled") return new Dictionary<string, object?> { ["message"] = "Đơn hàng đã bị hủy." };

            var model = new PaymentInformationModel
            {
                OrderType = "other",
                Amount = (double)order.TotalCost,
                OrderDescription = $"orderId:{orderId}",
                Name = "Khach hang"
            };

            var url = _vnPay.CreatePaymentUrl(model, context);

            return new Dictionary<string, object?> { ["message"] = "", ["paymentUrl"] = url };
        }

        public async Task<Dictionary<string, object?>> HandleCallbackAsync(IQueryCollection query)
        {
            // Đọc orderId từ query trước, không phụ thuộc vào response
            var rawOrderInfo = query.FirstOrDefault(k => k.Key == "vnp_OrderInfo").Value.ToString() ?? "";
            var vnpResponseCode = query.FirstOrDefault(k => k.Key == "vnp_ResponseCode").Value.ToString() ?? "";

            var match = System.Text.RegularExpressions.Regex.Match(rawOrderInfo, @"orderId:(\d+)");
            int.TryParse(match.Success ? match.Groups[1].Value : "", out var orderId);

            // Sau đó mới validate chữ ký
            var response = _vnPay.PaymentExecute(query);

            if (!response.Success)
                return new Dictionary<string, object?>
                {
                    ["success"] = false,
                    ["message"] = "Chữ ký không hợp lệ.",
                    ["orderId"] = orderId > 0 ? orderId : null,
                    ["code"] = vnpResponseCode
                };

            if (orderId == 0)
                return new Dictionary<string, object?>
                {
                    ["success"] = false,
                    ["message"] = "Không xác định được đơn hàng.",
                    ["orderId"] = null,
                    ["code"] = "99"
                };

            var order = await _repo.GetOrderByIdAsync(orderId);
            if (order == null)
                return new Dictionary<string, object?>
                {
                    ["success"] = false,
                    ["message"] = "NotFound",
                    ["orderId"] = orderId,
                    ["code"] = "99"
                };

            var isSuccess = vnpResponseCode == "00";

            _repo.AddPayment(new Payment
            {
                OrderId = orderId,
                TransactionId = response.TransactionId ?? "",
                PaymentMethod = order.PaymentMethod,
                Amount = order.TotalCost,
                VnPayResponseCode = vnpResponseCode,
                Success = isSuccess,
                CreatedAt = DateTime.UtcNow
            });

            if (isSuccess)
            {
                order.IsPaid = true;
                order.PaidAt = DateTime.UtcNow;
            }

            await _repo.SaveChangesAsync();

            return new Dictionary<string, object?>
            {
                ["success"] = isSuccess,
                ["message"] = isSuccess ? "" : "Thanh toán thất bại.",
                ["orderId"] = orderId,
                ["code"] = vnpResponseCode,
                ["transactionId"] = response.TransactionId
            };
        }

        public async Task<bool> ConfirmQrPaymentAsync(int orderId, decimal amount)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);
            if (order == null || order.IsPaid) return false;

            // Kiểm tra số tiền khớp (cho phép sai lệch ±1000đ)
            if (Math.Abs(order.TotalCost - amount) > 1000) return false;

            // Chỉ xác nhận đơn chưa hủy
            if (order.Status == "cancelled") return false;

            order.IsPaid = true;
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            return await _repo.SaveChangesAsync();
        }
    }
}