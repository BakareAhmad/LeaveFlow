using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Domain.Entities
{
    public class LeaveType : BaseEntity
    {

        public string Name { get; set; } = string.Empty;

        public int MaximumDays { get; set; }

        public string? Description { get; set; }

        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
