using LeaveFlow.Domain.Entities;
using System;

namespace LeaveFlow.Application.Dtos
{
    public record NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string? RelatedUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record AuditLogDto
    {
        public Guid Id { get; set; }
        public string Actor { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record OperationsReportFilterRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public record OperationsReportDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ScopeName { get; set; } = "All Departments";
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int Employees { get; set; }
        public int PendingLeave { get; set; }
        public int OpenTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int PendingExpenses { get; set; }
        public decimal ApprovedExpenseTotal { get; set; }
        public int OpenReviewCycles { get; set; }
        public int PendingPerformanceReviews { get; set; }
    }

    public record OrganisationAccessContextDto
    {
        public Guid UserId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid DepartmentId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsSystemAdmin { get; set; }
    }
}
