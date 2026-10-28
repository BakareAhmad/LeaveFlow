namespace LeaveFlow.Domain.Entities
{
    public class PerformanceReviewCycle : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsOpen { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<PerformanceReview> Reviews { get; set; } = new List<PerformanceReview>();
    }
}
