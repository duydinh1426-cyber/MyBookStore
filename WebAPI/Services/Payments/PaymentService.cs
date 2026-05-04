using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using Data.Vnpay;
using WebAPI.Services.Payments.VnPay;
using WebAPI.Services.Helper;

namespace WebAPI.Services.Payments
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

        public async Task<ServiceResult<object>> CreateVnPayUrlAsync(int userId, int orderId, HttpContext context)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.",404);
            if (order.UserId != userId)
                return ServiceResult<object>.Failure("Không có quyền truy cập.",403);
            if (order.IsPaid) 
                return ServiceResult<object>.Failure("Đơn hàng này đã được thanh toán.",400);
            if (order.Status == "cancelled")
                return ServiceResult<object>.Failure("Đơn hàng đã bị hủy.", 400);

            var model = new PaymentInformationModel
            {
                OrderType = "other",
                Amount = (double)order.TotalCost,
                OrderDescription = $"orderId:{orderId}",
                Name = "Khach hang"
            };

            var url = _vnPay.CreatePaymentUrl(model, context);
            return ServiceResult<object>.Success(new { paymentUrl = url });
        }

        public async Task<ServiceResult<object>> HandleCallbackAsync(IQueryCollection query)
        {
            // Đọc orderId từ query trước, không phụ thuộc vào response
            var rawOrderInfo = query["vnp_OrderInfo"].ToString() ?? "";
            var vnpResponseCode = query["vnp_ResponseCode"].ToString() ?? "";

            var match = System.Text.RegularExpressions.Regex.Match(rawOrderInfo, @"orderId:(\d+)");
            int.TryParse(match.Success ? match.Groups[1].Value : "", out var orderId);

            // Sau đó mới validate chữ ký
            var response = _vnPay.PaymentExecute(query);

            if (!response.Success)
                return ServiceResult<object>.Failure("Chữ ký không hợp lệ.", 400);

            if (orderId == 0)
                return ServiceResult<object>.Failure("Không xác định được đơn hàng.", 404);

            var order = await _repo.GetOrderByIdAsync(orderId);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.", 404);

            var isSuccess = vnpResponseCode == "00";

            _repo.AddPayment(new Payment
            {
                OrderId = orderId,
                TransactionId = response.TransactionId ?? "",
                PaymentMethod = order.PaymentMethod,
                Amount = order.TotalCost,
                VnPayResponseCode = vnpResponseCode,
                Success = isSuccess,
                CreatedAt = TimeHelper.NowVietnam()
            });

            if (isSuccess)
            {
                order.IsPaid = true;
                order.PaidAt = TimeHelper.NowVietnam();
            }

            await _repo.SaveChangesAsync();

            return ServiceResult<object>.Success(
                new
                {
                    orderId,
                    code = vnpResponseCode,
                    transactionId = response.TransactionId,
                    isSuccess
                },
                isSuccess ? "Thanh toán thành công" : "Thanh toán thất bại"
            );
        }

        public async Task<ServiceResult> ConfirmQrPaymentAsync(int orderId, decimal amount)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null)
                return ServiceResult.Failure("Không tìm thấy đơn hàng.", 404);

            if (order.IsPaid)
                return ServiceResult.Failure("Đơn hàng đã thanh toán.");

            if (order.Status == "cancelled")
                return ServiceResult.Failure("Đơn hàng đã bị hủy.", 400);

            if (Math.Abs(order.TotalCost - amount) > 1000)
                return ServiceResult.Failure("Số tiền không khớp.", 400);

            order.IsPaid = true;
            order.PaidAt = TimeHelper.NowVietnam();
            order.UpdatedAt = TimeHelper.NowVietnam();

            var success = await _repo.SaveChangesAsync();
            if (!success)
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Thanh toán thành công.");
        }
    }
}