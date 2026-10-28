using System;

namespace LeaveFlow.Domain.Entities
{
    public class PerformanceReview : BaseEntity
    {
        public Guid CycleId { get; set; }
        public PerformanceReviewCycle Cycle { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public Guid? ReviewerUserId { get; set; }
        public User? Reviewer { get; set; }

        public PerformanceReviewStatus Status { get; set; } = PerformanceReviewStatus.NotStarted;

        public string? ManagerComments { get; set; }

        public DateTime? ManagerReviewedAt { get; set; }

        public byte[] RowVersion { get; set; } = [];
    }
}
