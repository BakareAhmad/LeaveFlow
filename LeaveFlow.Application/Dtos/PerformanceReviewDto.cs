using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record PerformanceReviewCycleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsOpen { get; set; }
        public int TotalReviews { get; set; }
        public int CompletedReviews { get; set; }
        public int TotalDepartments { get; set; }
        public int SubmittedDepartmentReports { get; set; }
    }

    public record EmployeeTaskPerformanceDto
    {
        public int TotalCompletedTasks { get; set; }
        public int CompletedOnTime { get; set; }
        public int CompletedLate { get; set; }
        public double? OnTimeCompletionPercentage { get; set; }

        public bool HasCompletedTasks => TotalCompletedTasks > 0;

        public string SummaryText
        {
            get
            {
                if (!HasCompletedTasks)
                    return "No completed tasks during this review period.";

                return $"{CompletedOnTime} of {TotalCompletedTasks} completed tasks completed on time — {OnTimeCompletionPercentage:0.#}%";
            }
        }
    }

    public record EmployeeLeaveActivityDto
    {
        public int TotalNonAnnualLeaveRequests { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }
    }

    public record PerformanceReviewDto
    {
        public Guid Id { get; set; }
        public Guid CycleId { get; set; }
        public string CycleName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string Department { get; set; } = string.Empty;
        public string EmployeeRole { get; set; } = string.Empty;

        public Guid? ReviewerUserId { get; set; }
        public string? ReviewerName { get; set; }
        public PerformanceReviewStatus Status { get; set; }

        public EmployeeTaskPerformanceDto TaskPerformance { get; set; } = new();
        public EmployeeLeaveActivityDto LeaveActivity { get; set; } = new();

        public string? ManagerComments { get; set; }
        public DateTime? ManagerReviewedAt { get; set; }

        public bool IsDepartmentReportSubmitted { get; set; }
    }

    public record DepartmentReviewReportDto
    {
        public Guid CycleId { get; set; }
        public string CycleName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;

        public Guid? ManagerUserId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;

        public DateTime? SubmittedAt { get; set; }
        public bool IsSubmitted { get; set; }

        public int TotalEmployees { get; set; }
        public int ReviewsCompleted { get; set; }
        public double? AverageOnTimeCompletionPercentage { get; set; }
        public int TotalNonAnnualLeaveRequests { get; set; }
        public int TotalNonAnnualLeaveApproved { get; set; }
        public int TotalNonAnnualLeaveRejected { get; set; }

        public string? DepartmentComment { get; set; }

        public List<PerformanceReviewDto> EmployeeReviews { get; set; } = [];
    }

    public record CreatePerformanceCycleRequest
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
    }

    public record SaveManagerReviewRequest
    {
        [Required(ErrorMessage = "Manager review comment is required.")]
        [StringLength(4000)]
        public string ManagerComments { get; set; } = string.Empty;

        public bool MarkAsCompleted { get; set; } = true;
    }

    public record SubmitDepartmentReportRequest
    {
        [StringLength(4000)]
        public string? DepartmentComment { get; set; }
    }
}
