using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Domain.Entities
{
    public class LeaveRequest : BaseEntity
    {
        public Guid EmployeeId { get; set; }

        public Employee Employee { get; set; } = null!;

        public Guid LeaveTypeId { get; set; }

        public LeaveType LeaveType { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int NumberOfDays { get; set; }

        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        public Guid? ReviewedBy { get; set; }

        public User? Reviewer { get; set; }

        public DateTime? ReviewedDate { get; set; }

        public string? ReviewComment { get; set; }
    }
}
