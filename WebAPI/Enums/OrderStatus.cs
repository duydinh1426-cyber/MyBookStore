namespace WebAPI.Enums
{
    public enum OrderStatus
    {
        pending = 1,
        confirmed = 2,
        shipping = 3,
        completed = 4,
        cancelled = 5,
    }

    public static class OrderStatusExtensions
    {
        // Nhãn tiếng Việt - giữ nguyên vì đây là hiển thị người dùng
        public static string ToLabel(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.pending => "Chờ xác nhận",
                OrderStatus.confirmed => "Đã xác nhận",
                OrderStatus.shipping => "Đang giao",
                OrderStatus.completed => "Hoàn thành",
                OrderStatus.cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        // Giá trị string gửi frontend (giờ đã mặc định là chữ thường)
        public static string ToValue(this OrderStatus status)
        {
            return status.ToString();
        }

        // Parse string -> enum (không cần ToLower nữa vì enum đã là chữ thường)
        public static OrderStatus ToEnum(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return OrderStatus.pending;

            if (Enum.TryParse<OrderStatus>(value.Trim(), true, out var result))
                return result;

            return OrderStatus.pending;
        }

        // Kiểm tra logic chuyển đổi trạng thái
        public static bool CanTransitionTo(this OrderStatus current, OrderStatus target)
        {
            return (current, target) switch
            {
                (OrderStatus.pending, OrderStatus.confirmed) => true,
                (OrderStatus.pending, OrderStatus.cancelled) => true,
                (OrderStatus.confirmed, OrderStatus.shipping) => true,
                (OrderStatus.confirmed, OrderStatus.cancelled) => true,
                (OrderStatus.shipping, OrderStatus.completed) => true,
                (OrderStatus.shipping, OrderStatus.cancelled) => true,  // ← đảm bảo dòng này có
                _ => false
            };
        }

        public static List<OrderStatus> GetNextStatuses(this OrderStatus current)
        {
            return Enum.GetValues<OrderStatus>()
                .Where(next => current.CanTransitionTo(next))
                .ToList();
        }

        public static bool IsFinal(this OrderStatus status)
        {
            return status == OrderStatus.completed || status == OrderStatus.cancelled;
        }

        public static List<StatusOption> GetStatusList()
        {
            return Enum.GetValues<OrderStatus>()
                .Select(s => new StatusOption
                {
                    id = (int)s,
                    name = s.ToLabel(),
                    code = s.ToValue()
                }).ToList();
        }
    }

    public class StatusOption
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
    }
}