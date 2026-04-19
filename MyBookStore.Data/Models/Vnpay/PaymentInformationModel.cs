using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models.Vnpay
{
    public partial class PaymentInformationModel
    {
        public int OrderId { get; set; }
        public string? OrderType { get; set; }
        public double Amount { get; set; }
        public string? OrderDescription { get; set; }
        public string? Name { get; set; }
    }

}
