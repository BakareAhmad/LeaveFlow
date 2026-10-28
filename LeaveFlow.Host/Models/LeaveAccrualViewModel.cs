using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Host.Models
{
    public record LeaveAccrualViewModel
    {
        public LeaveAccrualOverviewDto Overview { get; set; } = new();
        public UpdateNextYearAccrualRequest NextYearAccrual { get; set; } = new();
    }
}
