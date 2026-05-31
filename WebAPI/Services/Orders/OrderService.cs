using MyBookStore.Data.Models;
using Data.Repositories.Interfaces;
using WebAPI.DTOs;
using WebAPI.Enums;
using WebAPI.Services.Helper;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        public OrderService(IOrderRepository repo) => _repo = repo;

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static void RestoreStock(Order order)
        {
            if (order.OrderItems == null) return;
            foreach (var item in order.OrderItems)
            {
                item.Book.NumberStock += item.Quantity;
                item.Book.NumberSold -= item.Quantity;
            }
        }

        private static bool IsOnlinePayment(string? method) =>
            method?.ToLower() is "vnpay" or "vietqr";

        // ─── Checkout ────────────────────────────────────────────────────────

        public async Task<ServiceResult<OrderResponseDto>> CheckoutAsync(int userId, CheckoutDto dto)
        {
            var method = dto.PaymentMethod.ToLower();
            if (method != "cod" && method != "vnpay" && method != "vietqr")
                return ServiceResult<OrderResponseDto>.Failure("Phương thức thanh toán không hợp lệ.", 400);

            var db = _repo.GetDbContext();
            var strategy = db.Database.CreateExecutionStrategy();

            ServiceResult<OrderResponseDto>? result = null;

            await strategy.ExecuteAsync(async () =>
            {
                // Bắt đầu transaction — đảm bảo toàn bộ check + trừ stock + lưu là atomic
                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // Load cart items với UPDLOCK: lock các book rows ngay lập tức
                    // → người B checkout cùng lúc sẽ phải chờ transaction này hoàn thành
                    var cartItems = await _repo.GetCartItemsWithLockAsync(userId);

                    if (!cartItems.Any())
                    {
                        result = ServiceResult<OrderResponseDto>.Failure("Giỏ hàng của bạn đang trống.", 400);
                        await transaction.RollbackAsync();
                        return;
                    }

                    decimal totalCost = 0;
                    var orderItems = new List<OrderItem>();
                    var outOfStockMessages = new List<string>();

                    foreach (var item in cartItems)
                    {
                        // Kiểm tra tồn kho TRONG transaction — giá trị này đã được lock
                        // nên phản ánh đúng số lượng thực tế sau các giao dịch khác
                        if (item.Book.NumberStock < item.Quantity)
                        {
                            if (item.Book.NumberStock <= 0)
                                outOfStockMessages.Add($"Sách '{item.Book.Title}' đã hết hàng.");
                            else
                                outOfStockMessages.Add(
                                    $"Sách '{item.Book.Title}' chỉ còn {item.Book.NumberStock} cuốn " +
                                    $"(bạn đang đặt {item.Quantity} cuốn).");
                        }
                    }

                    if (outOfStockMessages.Any())
                    {
                        result = ServiceResult<OrderResponseDto>.Failure(
                            string.Join(" ", outOfStockMessages), 400);
                        await transaction.RollbackAsync();
                        return;
                    }

                    // Tất cả sách đều còn đủ hàng — tiến hành trừ stock và tạo đơn
                    foreach (var item in cartItems)
                    {
                        totalCost += item.Book.Price * item.Quantity;
                        orderItems.Add(new OrderItem
                        {
                            BookId = item.BookId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Book.Price,
                            CreatedAt = TimeHelper.NowVietnam(),
                            UpdatedAt = TimeHelper.NowVietnam()
                        });

                        item.Book.NumberStock -= item.Quantity;
                        item.Book.NumberSold += item.Quantity;
                    }

                    var shippingFee = dto.ShippingFee >= 0 ? dto.ShippingFee : 0;

                    var order = new Order
                    {
                        UserId = userId,
                        Phone = dto.Phone.Trim(),
                        Address = dto.Address.Trim(),
                        Note = dto.Note?.Trim(),
                        Status = OrderStatus.pending.ToValue(),
                        PaymentMethod = method,
                        IsPaid = false,
                        ShippingFee = shippingFee,
                        TotalCost = totalCost + shippingFee,
                        OrderItems = orderItems,
                        CreatedAt = TimeHelper.NowVietnam(),
                        UpdatedAt = TimeHelper.NowVietnam()
                    };

                    _repo.AddOrder(order);
                    _repo.RemoveCartItems(cartItems);

                    if (!await _repo.SaveChangesAsync())
                    {
                        result = ServiceResult<OrderResponseDto>.Failure("Lỗi hệ thống khi xử lý đơn hàng.", 500);
                        await transaction.RollbackAsync();
                        return;
                    }

                    await transaction.CommitAsync();

                    result = ServiceResult<OrderResponseDto>.Success(new OrderResponseDto
                    {
                        orderId = order.OrderId,
                        totalCost = totalCost,
                        itemCount = orderItems.Count,
                        paymentMethod = method,
                        requiresPayment = IsOnlinePayment(method)
                    }, "Đặt hàng thành công.");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return result ?? ServiceResult<OrderResponseDto>.Failure("Lỗi hệ thống.", 500);
        }

        // ─── Read ────────────────────────────────────────────────────────────

        public async Task<ServiceResult<object>> GetByIdAsync(int userId, bool isAdmin, int id)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.", 404);

            if (!isAdmin && order.UserId != userId)
                return ServiceResult<object>.Failure("Bạn không có quyền truy cập.", 403);

            var currentStatus = order.Status.ToEnum();
            var data = new
            {
                orderId = order.OrderId,
                totalCost = order.TotalCost,
                status = order.Status,
                statusLabel = order.Status.ToEnum().ToLabel(),
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
                items = order.OrderItems?.Select(oi => new
                {
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

        public async Task<ServiceResult<object>> GetUserOrdersAsync(
            int userId, int page, int pageSize, string? status)
        {
            var (orders, total) = await _repo.GetUserOrdersAsync(userId, page, pageSize, status);

            var items = orders.Select(o => new
            {
                orderId = o.OrderId,
                totalCost = o.TotalCost,
                status = o.Status,
                statusLabel = o.Status.ToEnum().ToLabel(),
                phone = o.Phone,
                address = o.Address,
                note = o.Note,
                createdAt = o.CreatedAt,
                itemCount = o.OrderItems?.Count ?? 0,
                paymentMethod = o.PaymentMethod,
                isPaid = o.IsPaid
            });

            return ServiceResult<object>.Success(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                data = items
            });
        }

        public async Task<ServiceResult<object>> AdminGetAllOrdersAsync(
            string? status, string? keyword, int page, int pageSize)
        {
            var (orders, total) = await _repo.GetAllOrdersAdminAsync(status, keyword, page, pageSize);

            var data = orders.Select(o => new
            {
                orderId = o.OrderId,
                customerName = o.User?.Name ?? "",
                totalCost = o.TotalCost,
                status = o.Status,
                statusLabel = o.Status.ToEnum().ToLabel(),
                createdAt = o.CreatedAt,
                phone = o.Phone,
                paymentMethod = o.PaymentMethod,
                isPaid = o.IsPaid,
                address = o.Address
            });

            return ServiceResult<object>.Success(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                data
            });
        }

        public async Task<ServiceResult<object>> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _repo.GetOrderByIdAsync(orderId);

            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.", 404);

            if (order.UserId != userId)
                return ServiceResult<object>.Failure("Bạn không có quyền truy cập đơn hàng này.", 403);

            return ServiceResult<object>.Success(new
            {
                isPaid = order.IsPaid,
                status = order.Status
            });
        }

        // ─── Cancel ──────────────────────────────────────────────────────────

        public async Task<ServiceResult<object>> CancelAsync(int userId, int id, CancelOrderDto? dto = null)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.", 404);

            if (order.UserId != userId)
                return ServiceResult<object>.Failure("Bạn không có quyền truy cập.", 403);

            var current = order.Status.ToEnum();
            if (!current.CanTransitionTo(OrderStatus.cancelled))
                return ServiceResult<object>.Failure(
                    $"Không thể hủy đơn hàng đang ở trạng thái '{current.ToLabel()}'.", 400);

            RestoreStock(order);
            order.Status = OrderStatus.cancelled.ToValue();
            order.UpdatedAt = TimeHelper.NowVietnam();

            if (IsOnlinePayment(order.PaymentMethod) && order.IsPaid)
            {
                if (string.IsNullOrWhiteSpace(dto?.BankAccountNumber) ||
                    string.IsNullOrWhiteSpace(dto?.BankAccountName) ||
                    string.IsNullOrWhiteSpace(dto?.BankName))
                    return ServiceResult<object>.Failure(
                        "Vui lòng nhập đầy đủ thông tin ngân hàng để hoàn tiền.", 404);

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

                if (!await _repo.SaveChangesAsync())
                    return ServiceResult<object>.Failure("Lỗi hệ thống khi hủy đơn.", 500);

                return ServiceResult<object>.Success(
                    new { requiresRefund = true, refundAmount = order.TotalCost },
                    "Đơn hàng đã hủy. Yêu cầu hoàn tiền đã gửi đến admin.");
            }

            if (!await _repo.SaveChangesAsync())
                return ServiceResult<object>.Failure("Lỗi hệ thống khi hủy đơn.", 500);

            return ServiceResult<object>.Success(
                new { requiresRefund = false },
                "Hủy đơn hàng thành công.");
        }

        // ─── Admin: update status ─────────────────────────────────────────────

        public async Task<ServiceResult<object>> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _repo.GetOrderByIdAsync(id);
            if (order == null)
                return ServiceResult<object>.Failure("Không tìm thấy đơn hàng.", 404);

            var current = order.Status.ToEnum();
            var target = dto.GetStatus();

            if (!current.CanTransitionTo(target))
                return ServiceResult<object>.Failure(
                    $"Không thể chuyển từ '{current.ToLabel()}' sang '{target.ToLabel()}'.", 400);

            if (target == OrderStatus.confirmed && IsOnlinePayment(order.PaymentMethod) && !order.IsPaid)
                return ServiceResult<object>.Failure(
                    "Không thể xác nhận đơn hàng vì khách chưa thanh toán.", 400);

            if (target == OrderStatus.cancelled)
            {
                RestoreStock(order);

                if (IsOnlinePayment(order.PaymentMethod) && order.IsPaid)
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

            bool refundCreated = target == OrderStatus.cancelled
                && IsOnlinePayment(order.PaymentMethod)
                && order.IsPaid;

            order.Status = target.ToValue();
            order.UpdatedAt = TimeHelper.NowVietnam();

            if (!await _repo.SaveChangesAsync())
                return ServiceResult<object>.Failure("Lỗi hệ thống khi cập nhật.", 500);

            var data = new
            {
                orderId = order.OrderId,
                newStatus = order.Status,
                nextStatuses = target.GetNextStatuses().Select(s => s.ToValue()),
                isFinal = target.IsFinal(),
                refundCreated
            };

            string message = refundCreated
                ? "Đã hủy đơn. Yêu cầu hoàn tiền đã được tạo."
                : "Cập nhật trạng thái thành công.";

            return ServiceResult<object>.Success(data, message);
        }

        // ─── Admin: stats ─────────────────────────────────────────────────────

        public async Task<ServiceResult<object>> GetAdminStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var totalRevenue = await _repo.GetRevenueAsync(from, to);
            var monthlyRevenue = await _repo.GetMonthlyRevenueAsync();
            var totalOrders = await _repo.GetTotalOrdersAsync(from, to);
            var totalBooksSold = await _repo.GetTotalBooksSoldAsync(from, to);
            var statusStats = await _repo.GetStatusStatsAsync(from, to);

            return ServiceResult<object>.Success(new
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalOrders = totalOrders,
                TotalBooksSold = totalBooksSold,
                StatusDistribution = statusStats
            });
        }

        // ─── Refund ───────────────────────────────────────────────────────────

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
                return ServiceResult.Failure("Yêu cầu này đã được xử lý.", 400);

            r.Status = "completed";
            r.AdminNote = adminNote?.Trim();
            r.ResolvedAt = TimeHelper.NowVietnam();

            if (!await _repo.SaveChangesAsync())
                return ServiceResult.Failure("Lỗi hệ thống.", 500);

            return ServiceResult.Success("Đã đánh dấu hoàn tiền thành công.");
        }
    }
}