using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Domain.Enum
{
    public enum PaymentStatus
    {
        Pending = 1,
        Paid,
        Refunded
    }
}
