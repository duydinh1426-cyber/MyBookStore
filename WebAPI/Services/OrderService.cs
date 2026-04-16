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

        public OrderService(IOrderRepository repo)
        {
            _repo = repo;
        }

        private void RestoreStock(Order order)
        {
            foreach (var item in order.OrderItems)
            {
                item.Book.NumberStock += item.Quantity;
                item.Book.NumberSold -= item.Quantity;
            }
        }

        public async Task<ApiResponse<object>> Cancel(int userId, int id)
        {
            var order = await _repo.GetOrderById(id);

            if (order == null)
                return ApiResponse<object>.Fail("Không tìm thấy đơn", 404);

            if (order.UserId != userId)
                return ApiResponse<object>.Fail("Không có quyền", 403);

            var current = order.Status.ToEnum();

            if (!current.CanTransitionTo(OrderStatus.CANCELLED))
                return ApiResponse<object>.Fail($"Không thể hủy đơn ở trạng thái '{current.ToLabel()}'");

            RestoreStock(order);

            order.Status = OrderStatus.CANCELLED.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            await _repo.SaveChangesAsync();

            return ApiResponse<object>.Success(null, "Hủy đơn hàng thành công.");
        }

        public async Task<ApiResponse<object>> Checkout(int userId, CheckoutDto dto)
        {
            var cartItems = await _repo.GetCartItems(userId);

            if (!cartItems.Any())
                return ApiResponse<object>.Fail("Giỏ hàng trống");

            decimal total = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                if (item.Book.NumberStock < item.Quantity)
                    return ApiResponse<object>.Fail($"Sách '{item.Book.Title}' không đủ hàng");

                total += item.Book.Price * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Book.Price,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });

                item.Book.NumberStock -= item.Quantity;
                item.Book.NumberSold += item.Quantity;
            }

            var order = new Order
            {
                UserId = userId,
                Phone = dto.Phone,
                Address = dto.Address,
                Note = dto.Note,
                Status = OrderStatus.PENDING.ToValue(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalCost = total,
                OrderItems = orderItems
            };

            await _repo.AddOrder(order);
            _repo.RemoveCartItems(cartItems);

            return ApiResponse<object>.Success(new
            {
                orderId = order.OrderId,
                totalCost = total
            }, "Đặt hàng thành công");
        }

        public async Task<ApiResponse<object>> GetById(int userId, bool isAdmin, int id)
        {
            var order = await _repo.GetOrderById(id);

            if (order == null)
                return ApiResponse<object>.Fail("Không tìm thấy đơn hàng", 404);

            if (!isAdmin && order.UserId != userId)
                return ApiResponse<object>.Fail("Khôn có quyền", 403);

            var current = order.Status.ToEnum();

            return ApiResponse<object>.Success(new
            {
                order.OrderId,
                order.TotalCost,
                order.Status,
                statusLabel = current.ToLabel(),
                nextStatuses = current.GetNextStatuses()
                                      .Select(s => s.ToValue()),
                isFinal = current.isFinal()
            });
        }

        public async Task<ApiResponse<object>> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var order = await _repo.GetOrderById(id);

            if (order == null)
                return ApiResponse<object>.Fail("Không tìm thấy đơn", 404);

            var current = order.Status.ToEnum();

            if (!current.CanTransitionTo(dto.Status))
            {
                var next = current.GetNextStatuses()
                                  .Select(s => s.ToLabel());

                return ApiResponse<object>.Fail($"Chỉ được chuyển sang: {string.Join(", ", next)}");
            }

            if (dto.Status == OrderStatus.CANCELLED)
                RestoreStock(order);

            order.Status = dto.Status.ToValue();
            order.UpdatedAt = DateTime.UtcNow;

            await _repo.SaveChangesAsync();

            return ApiResponse<object>.Success(new
            {
                message = "Cập nhật thành công",
                nextStatus = dto.Status.GetNextStatuses()
                                       .Select(s => s.ToValue()),
                isFinal = dto.Status.isFinal()
            });
        }
    }
}
