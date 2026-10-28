using LeaveFlow.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record LeaveRequestDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public Guid LeaveTypeId { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NumberOfDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveStatus Status { get; set; }
        public DateTime AppliedDate { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewComment { get; set; }
    }

    public record ApplyLeaveRequest
    {
        [Required]
        public Guid LeaveTypeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public record UpdateLeaveRequest
    {
        [Required]
        public Guid LeaveTypeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public record ReviewLeaveRequest
    {
        [Required]
        [EnumDataType(typeof(LeaveStatus))]
        public LeaveStatus Status { get; set; }

        [StringLength(500)]
        public string? ReviewComment { get; set; }
    }
}
