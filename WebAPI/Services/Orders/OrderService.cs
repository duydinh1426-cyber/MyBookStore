using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Helper;

namespace WebAPI.Services.Orders
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

        public async Task<ServiceResult<OrderResponseDto>> CheckoutAsync(int userId, CheckoutDto dto)
        {
            var cartItems = await _repo.GetCartItemsAsync(userId);
            if (!cartItems.Any()) 
                return ServiceResult<OrderResponseDto>.Failure("Giỏ hàng của bạn đang trống.");

            var method = dto.PaymentMethod.ToLower();
            if (method != "cod" && method != "vnpay" && method != "vietqr")
                return ServiceResult<OrderResponseDto>.Failure("Phương thức thanh toán không hợp lệ.");

            decimal totalCost = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                if (item.Book.NumberStock < item.Quantity)
                    return ServiceResult<OrderResponseDto>.Failure($"Sách '{item.Book.Title}' chỉ còn {item.Book.NumberStock} cuốn.");

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
            {
                var data = new OrderResponseDto
                {
                    orderId = order.OrderId,
                    totalCost = totalCost,
                    itemCount = orderItems.Count,
                    paymentMethod = method,
                    // Nếu vnpay thì frontend cần gọi tiếp /api/payment/vnpay/create
                    requiresPayment = method == "vnpay" || method == "vietqr"
                };
                return ServiceResult<OrderResponseDto>.Success(data, "Đặt hàng thành công.");
            }

            return ServiceResult<OrderResponseDto>.Failure("Lỗi hệ thống khi xử lý đơn hàng.");
        }

        public async Task<ServiceResult<object>> GetByIdAsync(int userId, bool isAdmin, int id)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng");

            if (!isAdmin && order.UserId != userId)
                return ServiceResult<object>.Failure("Bạn không có quyền truy cập.", 403);

            var currentStatus = order.Status.ToEnum();
            var data = new
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
            return ServiceResult<object>.Success(data); 
        }

        public async Task<ServiceResult<object>> GetUserOrdersAsync(int userId, int page, int pageSize, string? status)
        {
            var orders = await _repo.GetUserOrdersAsync(userId, page, pageSize, status);
            return ServiceResult<object>.Success(orders);
        }

        public async Task<ServiceResult<object>> AdminGetAllOrdersAsync(string? status, string? keyword, int page, int pageSize)
        {
            var data = await _repo.GetAllOrdersAdminAsync(status, keyword, page, pageSize);
            return ServiceResult<object>.Success(data);
        }

        public async Task<ServiceResult<object>> CancelAsync(int userId, int id, CancelOrderDto? dto = null)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.");
            if (order.UserId != userId)
                return ServiceResult<object>.Failure("Bạn không có quyền truy cập");

            var current = order.Status.ToEnum();
            if (!current.CanTransitionTo(OrderStatus.cancelled))
                return ServiceResult<object>.Failure($"Không thể hủy đơn hàng đang ở trạng thái '{current.ToLabel()}'.");

            RestoreStock(order);
            order.Status = OrderStatus.cancelled.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            if ((order.PaymentMethod?.ToLower() == "vnpay" || order.PaymentMethod?.ToLower() == "vietqr")
            && order.IsPaid)
            {
                // Validate thông tin ngân hàng
                if (string.IsNullOrWhiteSpace(dto?.BankAccountNumber) ||
                    string.IsNullOrWhiteSpace(dto?.BankAccountName) ||
                    string.IsNullOrWhiteSpace(dto?.BankName))
                    return ServiceResult<object>.Failure("Vui lòng nhập đầy đủ thông tin ngân hàng để hoàn tiền.");

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
                    CreatedAt = TimeHelper.NowVietnam()
                }); 

                if (await _repo.SaveChangesAsync())
                {
                    var data = new
                    {
                        requiresRefund = true,
                        refundAmount = order.TotalCost
                    };
                    return ServiceResult<object>.Success(data, "Đơn hàng đã hủy. Yêu cầu hoàn tiền đã gửi đến admin.");
                }    
            }
            else
            {
                if (await _repo.SaveChangesAsync())
                    return ServiceResult<object>.Success(new { requiresRefund = false }, "Hủy đơn hàng thành công.");
            }

            return ServiceResult<object>.Failure("Lỗi hệ thống khi hủy đơn.", 500);
        }

        public async Task<ServiceResult<object>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.");

            var current = order.Status.ToEnum();
            var target = dto.GetStatus();

            if (!current.CanTransitionTo(target))
                return ServiceResult<object>.Failure($"Không thể chuyển từ '{current.ToLabel()}' sang '{target.ToLabel()}'");

            if (target == OrderStatus.confirmed
                && (order.PaymentMethod?.ToLower() == "vnpay" || order.PaymentMethod?.ToLower() == "vietqr")
                && !order.IsPaid)
                return ServiceResult<object>.Failure("Không thể xác nhận đơn hàng vì khách chưa thanh toán.");

            if (target == OrderStatus.cancelled)
            {
                RestoreStock(order);
                if (order.PaymentMethod?.ToLower() is "vnpay" or "vietqr" && order.IsPaid)
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
                        CreatedAt = TimeHelper.NowVietnam()
                    });
                }
            }

            order.Status = target.ToValue();
            order.UpdatedAt = TimeHelper.NowVietnam();

            if (await _repo.SaveChangesAsync())
            {
                var refundCreated = target == OrderStatus.cancelled
                    && order.PaymentMethod?.ToLower() is "vnpay" or "vietqr"
                    && order.IsPaid;  // IsPaid vẫn đúng tại thời điểm này

                var data = new
                {
                    orderId = order.OrderId,
                    newStatus = order.Status,
                    nextStatuses = target.GetNextStatuses().Select(s => s.ToValue()),
                    isFinal = target.IsFinal(),
                    refundCreated 
                };

                string mess = refundCreated ? "Đã hủy đơn. Yêu cầu hoàn tiền đã được tạo."
                                               : "Cập nhật trạng thái thành công";
                return ServiceResult<object>.Success(data, mess);
            }

            return ServiceResult<object>.Failure("Lỗi hệ thống khi cập nhật.", 500);
        }

        public async Task<ServiceResult<object>> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var data = await _repo.GetAdminStatsAsync(from, to);
            return ServiceResult<object>.Success(data);
        }

		public async Task<ServiceResult<object>> GetRefundRequestsAsync(string? status)
		{
			var list = await _repo.GetRefundRequestsAsync(status);
			var data = list.Select(r => new
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
            return ServiceResult<object>.Success(data);
		}

		public async Task<ServiceResult> ResolveRefundAsync(int refundId, string? adminNote)
		{
			var r = await _repo.GetRefundRequestByIdAsync(refundId);
            if (r == null)
                return ServiceResult.Failure("NotFound", 404);
			if (r.Status == "completed")
                return ServiceResult.Failure("Yêu cầu này đã được xử lý.");

			r.Status = "completed";
			r.AdminNote = adminNote?.Trim();
            r.ResolvedAt = TimeHelper.NowVietnam();

			if (await _repo.SaveChangesAsync())
				return ServiceResult.Success("Đã đánh dấu hoàn tiền thành công.");

			return ServiceResult.Failure("Lỗi hệ thống.");
		}

        public async Task<ServiceResult<object>> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.", 404);

            if (order.UserId != userId)
                return ServiceResult<object>.Failure("Bạn không có quyền truy cập đơn hàng này.", 403);

            var data = new
            {
                isPaid = order.IsPaid,
                status = order.Status
            };

            return ServiceResult<object>.Success(data);
        }
    }
}