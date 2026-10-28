using System;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record LeaveAccrualSettingDto
    {
        public int Year { get; set; }
        public int AccrualAmount { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public record LeaveAccrualOverviewDto
    {
        public int CurrentYear { get; set; }
        public LeaveAccrualSettingDto? CurrentYearSetting { get; set; }
        public int NextYear { get; set; }
        public LeaveAccrualSettingDto? NextYearSetting { get; set; }
    }

    public record UpdateNextYearAccrualRequest
    {
        [Range(1, 365)]
        public int AccrualAmount { get; set; }
    }
}
