namespace LeaveFlow.Domain.Entities
{
    public class LeaveAccrualSetting : BaseEntity
    {
        public int Year { get; set; }

        public int AccrualAmount { get; set; }

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
