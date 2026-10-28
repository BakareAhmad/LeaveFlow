using System;

namespace LeaveFlow.Domain.Entities
{
    public class DepartmentReviewReport : BaseEntity
    {
        public Guid CycleId { get; set; }
        public PerformanceReviewCycle Cycle { get; set; } = null!;

        public Guid DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public Guid? ManagerUserId { get; set; }
        public User? ManagerUser { get; set; }

        public string? DepartmentComment { get; set; }

        public bool IsSubmitted { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }
}
