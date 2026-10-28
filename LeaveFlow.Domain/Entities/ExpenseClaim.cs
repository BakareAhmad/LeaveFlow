namespace LeaveFlow.Domain.Entities
{
    public class ExpenseClaim : BaseEntity
    {
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public Guid DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
        public ExpenseClaimStatus Status { get; set; } = ExpenseClaimStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public Guid? AdminReviewedBy { get; set; }
        public User? AdminReviewer { get; set; }
        public DateTime? AdminReviewedAt { get; set; }
        public string? AdminComment { get; set; }
        public Guid? WorkTaskId { get; set; }
        public WorkTask? WorkTask { get; set; }
        public byte[] RowVersion { get; set; } = [];
        public ICollection<ExpenseItem> Items { get; set; } = new List<ExpenseItem>();
        public ICollection<ExpenseAttachment> Attachments { get; set; } = new List<ExpenseAttachment>();
    }
}
