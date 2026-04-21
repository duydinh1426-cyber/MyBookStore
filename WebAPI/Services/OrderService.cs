using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        public OrderService(IOrderRepository repo) => _repo = repo;

        private void RestoreStock(Order order)
        {
            if (order.OrderItems == null) return;
            foreach (var item in order.OrderItems)
            {
                item.Book.NumberStock += item.Quantity;
                item.Book.NumberSold -= item.Quantity;
            }
        }

        public async Task<object> CheckoutAsync(int userId, CheckoutDto dto)
        {
            var cartItems = await _repo.GetCartItemsAsync(userId);
            if (!cartItems.Any()) return new { message = "Giỏ hàng của bạn đang trống." };

            var method = dto.PaymentMethod.ToLower();
            if (method != "cod" && method != "vnpay")
                return new { message = "Phương thức thanh toán không hợp lệ." };

            decimal totalCost = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                if (item.Book.NumberStock < item.Quantity)
                    return new { message = $"Sách '{item.Book.Title}' chỉ còn {item.Book.NumberStock} cuốn." };

                totalCost += item.Book.Price * item.Quantity;
                orderItems.Add(new OrderItem
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Book.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                item.Book.NumberStock -= item.Quantity;
                item.Book.NumberSold += item.Quantity;
            }

            var order = new Order
            {
                UserId = userId,
                Phone = dto.Phone.Trim(),
                Address = dto.Address.Trim(),
                Note = dto.Note?.Trim(),
                Status = OrderStatus.pending.ToValue(),
                PaymentMethod = method,
                IsPaid = false,
                TotalCost = totalCost,
                OrderItems = orderItems,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _repo.AddOrder(order);
            _repo.RemoveCartItems(cartItems);

            if (await _repo.SaveChangesAsync())
                return new
                {
                    message = "Đặt hàng thành công.",
                    orderId = order.OrderId,
                    totalCost = totalCost, 
                    itemCount = orderItems.Count,
                    paymentMethod = method,
                    // Nếu vnpay thì frontend cần gọi tiếp /api/payment/vnpay/create
                    requiresPayment = method == "vnpay"
                };

            return new { message = "Lỗi hệ thống khi xử lý đơn hàng." };
        }

        public async Task<object?> GetByIdAsync(int userId, bool isAdmin, int id)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null) return null;

            if (!isAdmin && order.UserId != userId) return new { message = "Forbidden" };

            var currentStatus = order.Status.ToEnum();
            return new
            {
                message = "",
                orderId = order.OrderId,
                totalCost = order.TotalCost,
                status = order.Status,
                phone = order.Phone,
                address = order.Address,
                note = order.Note,
                createdAt = order.CreatedAt,
                updatedAt = order.UpdatedAt,
                nextStatuses = currentStatus.GetNextStatuses().Select(s => s.ToValue()),
                isFinal = currentStatus.IsFinal(),
                customer = new
                {
                    userId = order.UserId,
                    name = order.User?.Name,
                    email = order.User?.Account?.Email ?? ""
                },
                items = order.OrderItems?.Select(oi => new {
                    orderItemId = oi.OrderItemId,
                    quantity = oi.Quantity,
                    unitPrice = oi.UnitPrice,
                    subTotal = oi.Quantity * oi.UnitPrice,
                    book = new
                    {
                        bookId = oi.BookId,
                        title = oi.Book.Title,
                        author = oi.Book.Author,
                        image = oi.Book.Image
                    }
                })
            };
        }

        public async Task<object> GetUserOrdersAsync(int userId, int page, int pageSize, string? status)
        {
            var orders = await _repo.GetUserOrdersAsync(userId, page, pageSize, status);
            return orders;
        }

        public async Task<object> AdminGetAllOrdersAsync(string? status, string? keyword, int page, int pageSize)
        {
            return await _repo.GetAllOrdersAdminAsync(status, keyword, page, pageSize);
        }

        public async Task<object> CancelAsync(int userId, int id, CancelOrderDto? dto = null)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null) return new { message = "NotFound" };
            if (order.UserId != userId) return new { message = "Forbidden" };

            var current = order.Status.ToEnum();
            if (!current.CanTransitionTo(OrderStatus.cancelled))
                return new { message = $"Không thể hủy đơn hàng đang ở trạng thái '{current.ToLabel()}'." };

            RestoreStock(order);
            order.Status = OrderStatus.cancelled.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            if (order.PaymentMethod?.ToLower() == "vnpay" && order.IsPaid)
            {
                // Validate thông tin ngân hàng
                if (string.IsNullOrWhiteSpace(dto?.BankAccountNumber) ||
                    string.IsNullOrWhiteSpace(dto?.BankAccountName) ||
                    string.IsNullOrWhiteSpace(dto?.BankName))
                    return new { message = "Vui lòng nhập đầy đủ thông tin ngân hàng để hoàn tiền." };

                _repo.AddRefundRequest(new RefundRequest
                {
                    OrderId = order.OrderId,
                    UserId = userId,
                    Amount = order.TotalCost,
                    Note = dto.Note?.Trim(),
                    BankAccountNumber = dto.BankAccountNumber.Trim(),
                    BankAccountName = dto.BankAccountName.Trim().ToUpper(),
                    BankName = dto.BankName.Trim(),
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                });

                if (await _repo.SaveChangesAsync())
                    return new
                    {
                        message = "Đơn hàng đã hủy. Yêu cầu hoàn tiền đã gửi đến admin.",
                        requiresRefund = true,
                        refundAmount = order.TotalCost
                    };
            }
            else
            {
                if (await _repo.SaveChangesAsync())
                    return new { message = "Hủy đơn hàng thành công.", requiresRefund = false };
            }

            return new { message = "Lỗi hệ thống khi hủy đơn." };
        }

        public async Task<object> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null) return new { success = false, message = "NotFound" };

            var current = order.Status.ToEnum();
            var target = dto.GetStatus();

            if (!current.CanTransitionTo(target))
                return new { success = false, message = $"Không thể chuyển từ '{current.ToLabel()}' sang '{target.ToLabel()}'" };

            if (target == OrderStatus.confirmed
                && order.PaymentMethod?.ToLower() == "vnpay"
                && !order.IsPaid)
                return new
                {
                    success = false,
                    message = "Không thể xác nhận đơn hàng vì khách chưa thanh toán qua VNPay."
                };

            if (target == OrderStatus.cancelled)
            {
                RestoreStock(order);

                // Admin hủy đơn VNPay đã thanh toán → tự động tạo RefundRequest
                if (order.PaymentMethod?.ToLower() == "vnpay" && order.IsPaid)
                {
                    _repo.AddRefundRequest(new RefundRequest
                    {
                        OrderId = order.OrderId,
                        UserId = order.UserId,
                        Amount = order.TotalCost,
                        Note = "Admin hủy đơn hàng — cần liên hệ khách để lấy thông tin ngân hàng",
                        BankAccountNumber = "",
                        BankAccountName = "",
                        BankName = "",
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            order.Status = target.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            if (await _repo.SaveChangesAsync())
                return new
                {
                    success = true,
                    message = target == OrderStatus.cancelled
                        && order.PaymentMethod?.ToLower() == "vnpay"
                        && order.IsPaid
                            ? "Đã hủy đơn hàng. Yêu cầu hoàn tiền đã được tạo, vui lòng liên hệ khách để lấy thông tin ngân hàng."
                            : "Cập nhật trạng thái thành công",
                    orderId = order.OrderId,
                    newStatus = order.Status,
                    nextStatuses = target.GetNextStatuses().Select(s => s.ToValue()),
                    isFinal = target.IsFinal()
                };

            return new { success = false, message = "Lỗi hệ thống khi cập nhật" };
        }

        public async Task<object> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            return await _repo.GetAdminStatsAsync(from, to);
        }

		public async Task<object> GetRefundRequestsAsync(string? status)
		{
			var list = await _repo.GetRefundRequestsAsync(status);
			return list.Select(r => new
			{
				r.RefundRequestId,
				r.OrderId,
				r.Amount,
				r.Note,
				r.Status,
				r.AdminNote,
				r.CreatedAt,
				r.ResolvedAt,
                r.BankAccountNumber,
                r.BankAccountName,
                r.BankName,
                hasBankInfo = !string.IsNullOrEmpty(r.BankAccountNumber),
                customer = new
				{
					r.User?.Name,
					phone = r.Order?.Phone,
					email = r.User?.Account?.Email
				}
			});
		}

		public async Task<object> ResolveRefundAsync(int refundId, string? adminNote)
		{
			var r = await _repo.GetRefundRequestByIdAsync(refundId);
			if (r == null) return new { success = false, message = "NotFound" };
			if (r.Status == "completed") return new { success = false, message = "Yêu cầu này đã được xử lý." };

			r.Status = "completed";
			r.AdminNote = adminNote?.Trim();
			r.ResolvedAt = DateTime.UtcNow;

			if (await _repo.SaveChangesAsync())
				return new { success = true, message = "Đã đánh dấu hoàn tiền thành công." };

			return new { success = false, message = "Lỗi hệ thống." };
		}
	}
}