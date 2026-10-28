using System;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record LeaveTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaximumDays { get; set; }
        public string? Description { get; set; }
    }

    public record CreateLeaveTypeRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 365)]
        public int MaximumDays { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public record UpdateLeaveTypeRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 365)]
        public int MaximumDays { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
