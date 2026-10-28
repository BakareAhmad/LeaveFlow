namespace LeaveFlow.Domain.Entities
{
    public class WorkTask : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Assigned;
        public Guid DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        public Guid AssignedEmployeeId { get; set; }
        public Employee AssignedEmployee { get; set; } = null!;
        public Guid AssignedByUserId { get; set; }
        public User AssignedByUser { get; set; } = null!;
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewComment { get; set; }
        public byte[] RowVersion { get; set; } = [];
        public ICollection<TaskProgressUpdate> ProgressUpdates { get; set; } = new List<TaskProgressUpdate>();
    }
}
