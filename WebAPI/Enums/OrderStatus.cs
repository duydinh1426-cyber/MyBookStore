namespace WebAPI.Enums
{
    public enum OrderStatus
    {
        PENDING = 1,
        CONFIRMED = 2,
        SHIPPING = 3,
        COMPLETED = 4,
        CANCELLED = 5,
    }

    public static class OrderStatusExtensions
    {
        // label tiếng việt 
        public static string ToLabel(this OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.PENDING: return "Chờ xác nhận";
                case OrderStatus.CONFIRMED: return "Đã xác nhận";
                case OrderStatus.SHIPPING: return "Đang giao";
                case OrderStatus.COMPLETED: return "Hoàn thành";
                case OrderStatus.CANCELLED: return "Đã hủy";
                default: return "Không xác định";
            }
        }

        // value string gửi frontend
        public static string ToValue(this OrderStatus status)
        {
            return status.ToString().ToLower();
        }

        // parse string -> enum
        public static OrderStatus ToEnum(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return OrderStatus.PENDING;

            if (Enum.TryParse<OrderStatus>(value.Trim(), true, out var result))
                return result;

            return OrderStatus.PENDING;
        }

        // kiểm tra trạng thái
        public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
        {
            if (current == OrderStatus.PENDING)
                return next == OrderStatus.CONFIRMED || next == OrderStatus.CANCELLED;

            if (current == OrderStatus.CONFIRMED)
                return next == OrderStatus.SHIPPING || next == OrderStatus.CANCELLED;

            if (current == OrderStatus.SHIPPING)
                return next == OrderStatus.COMPLETED || next == OrderStatus.CANCELLED;

            return false;
        }

        // danh sách trạng thái tiếp theo
        public static List<OrderStatus> GetNextStatuses(this OrderStatus current)
        {
            return Enum.GetValues<OrderStatus>()
                .Where(next => current.CanTransitionTo(next))
                .ToList();
        }

        public static bool isFinal(this OrderStatus status)
        {
            return status == OrderStatus.COMPLETED || status == OrderStatus.CANCELLED;
        }

        // trả về danh sách cho dropdown
        public static List<StatusOption> GetStatusList()
        {
            var list = new List<StatusOption>();

            foreach (OrderStatus s in Enum.GetValues(typeof(OrderStatus)))
            {
                list.Add(new StatusOption
                {
                    Id = (int)s,
                    Name = s.ToLabel(),
                    Code = s.ToValue()
                });
            }

            return list;
        }
    }

    public class StatusOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
