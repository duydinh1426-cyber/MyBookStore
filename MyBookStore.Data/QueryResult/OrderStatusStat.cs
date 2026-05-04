using System;
using System.Collections.Generic;
using System.Text;

namespace Data.QueryResult
{
    public class OrderStatusStat
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }
}
