using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Domain.Enum
{
    public enum OrderStatus
    {
        Pending = 1,
        Processing,
        Completed,
        Cancelled,
    }
}
