namespace LeaveFlow.Domain.Entities
{
    public class ExpenseAttachment : BaseEntity
    {
        public Guid ExpenseClaimId { get; set; }
        public ExpenseClaim ExpenseClaim { get; set; } = null!;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
