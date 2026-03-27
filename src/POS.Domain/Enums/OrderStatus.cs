using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Cancelled = 4,
        Hold = 5,
    }
}