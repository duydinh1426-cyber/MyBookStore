using Data.Repositories.Interfaces;
using MyBookStore.Data.Models;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Interfaces;

namespace WebAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        public OrderService(IOrderRepository repo) => _repo = repo;

        #region Helpers
        private void RestoreStock(Order order)
        {
            foreach (var item in order.OrderItems!)
            {
                item.Book.NumberStock += item.Quantity;
                item.Book.NumberSold -= item.Quantity;
            }
        }
        #endregion

        public async Task<ApiResponse<object>> CheckoutAsync(int userId, CheckoutDto dto)
        {
            var cartItems = await _repo.GetCartItemsAsync(userId);
            if (!cartItems.Any()) return ApiResponse<object>.Fail("Giỏ hàng của bạn đang trống.");

            decimal totalCost = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                if (item.Book.NumberStock < item.Quantity)
                    return ApiResponse<object>.Fail($"Sách '{item.Book.Title}' chỉ còn {item.Book.NumberStock} cuốn.");

                totalCost += item.Book.Price * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Book.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                // Cập nhật kho
                item.Book.NumberStock -= item.Quantity;
                item.Book.NumberSold += item.Quantity;
            }

            var order = new Order
            {
                UserId = userId,
                Phone = dto.Phone.Trim(),
                Address = dto.Address.Trim(),
                Note = dto.Note?.Trim(),
                Status = OrderStatus.PENDING.ToValue(),
                TotalCost = totalCost,
                OrderItems = orderItems,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _repo.AddOrder(order);
            _repo.RemoveCartItems(cartItems);

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(new { orderId = order.OrderId, total = totalCost }, "Đặt hàng thành công.")
                : ApiResponse<object>.Fail("Lỗi hệ thống khi xử lý đơn hàng.", 500);
        }

        public async Task<ApiResponse<object>> GetByIdAsync(int userId, bool isAdmin, int id)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null) return ApiResponse<object>.Fail("Không tìm thấy đơn hàng.", 404);

            if (!isAdmin && order.UserId != userId)
                return ApiResponse<object>.Fail("Bạn không có quyền xem đơn hàng này.", 403);

            var currentStatus = order.Status.ToEnum();

            return ApiResponse<object>.Success(new
            {
                order.OrderId,
                order.Phone,
                order.Address,
                order.TotalCost,
                Status = order.Status,
                StatusLabel = currentStatus.ToLabel(),
                NextStatuses = currentStatus.GetNextStatuses().Select(s => s.ToValue()),
                IsFinal = currentStatus.isFinal(),
                Items = order.OrderItems?.Select(oi => new
                {
                    oi.BookId,
                    oi.Book.Title,
                    oi.Quantity,
                    oi.UnitPrice,
                    SubTotal = oi.Quantity * oi.UnitPrice
                })
            });
        }

        public async Task<ApiResponse<object>> GetUserOrdersAsync(int userId)
        {
            var orders = await _repo.GetUserOrdersAsync(userId);
            var data = orders.Select(o => new
            {
                o.OrderId,
                o.TotalCost,
                o.Status,
                StatusLabel = o.Status.ToEnum().ToLabel(),
                o.CreatedAt
            });
            return ApiResponse<object>.Success(data);
        }

        public async Task<ApiResponse<object>> AdminGetAllOrdersAsync(string? status, string? keyword)
        {
            var orders = await _repo.GetAllOrdersAdminAsync(status, keyword);
            var data = orders.Select(o => new
            {
                o.OrderId,
                CustomerName = o.User?.Name,
                o.TotalCost,
                o.Status,
                StatusLabel = o.Status.ToEnum().ToLabel(),
                o.CreatedAt,
                o.Phone
            });
            return ApiResponse<object>.Success(data);
        }

        public async Task<ApiResponse<object>> CancelAsync(int userId, int id)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null) return ApiResponse<object>.Fail("Không tìm thấy đơn hàng.", 404);
            if (order.UserId != userId) return ApiResponse<object>.Fail("Bạn không có quyền hủy đơn này.", 403);

            var current = order.Status.ToEnum();
            if (!current.CanTransitionTo(OrderStatus.CANCELLED))
                return ApiResponse<object>.Fail($"Không thể hủy đơn hàng đang ở trạng thái '{current.ToLabel()}'.");

            RestoreStock(order);
            order.Status = OrderStatus.CANCELLED.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(null, "Hủy đơn hàng thành công.")
                : ApiResponse<object>.Fail("Lỗi khi cập nhật trạng thái đơn hàng.", 500);
        }

        public async Task<ApiResponse<object>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null) return ApiResponse<object>.Fail("Không tìm thấy đơn hàng.", 404);

            var current = order.Status.ToEnum();
            if (!current.CanTransitionTo(dto.Status))
            {
                var allowed = current.GetNextStatuses().Select(s => s.ToLabel());
                return ApiResponse<object>.Fail($"Trạng thái không hợp lệ. Chỉ có thể chuyển sang: {string.Join(", ", allowed)}");
            }

            if (dto.Status == OrderStatus.CANCELLED)
                RestoreStock(order);

            order.Status = dto.Status.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            return await _repo.SaveChangesAsync()
                ? ApiResponse<object>.Success(new
                {
                    message = "Cập nhật thành công",
                    newStatus = dto.Status.ToValue(),
                    isFinal = dto.Status.isFinal()
                })
                : ApiResponse<object>.Fail("Lỗi khi cập nhật trạng thái.", 500);
        }

        public async Task<ApiResponse<object>> GetAdminStatsAsync()
        {
            try
            {
                var stats = await _repo.GetAdminStatsAsync();
                return ApiResponse<object>.Success(stats);
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail("Lỗi khi lấy dữ liệu thống kê.", 500);
            }
        }
    }
}