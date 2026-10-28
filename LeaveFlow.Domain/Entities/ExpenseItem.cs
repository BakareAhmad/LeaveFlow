namespace LeaveFlow.Domain.Entities
{
    public class ExpenseItem : BaseEntity
    {
        public Guid ExpenseClaimId { get; set; }
        public ExpenseClaim ExpenseClaim { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
