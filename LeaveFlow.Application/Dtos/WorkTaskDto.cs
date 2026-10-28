using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record WorkTaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskPriority Priority { get; set; }
        public WorkTaskStatus Status { get; set; }
        public Guid DepartmentId { get; set; }
        public string Department { get; set; } = string.Empty;
        public Guid AssignedEmployeeId { get; set; }
        public string AssignedEmployee { get; set; } = string.Empty;
        public string AssignedEmployeeRole { get; set; } = string.Empty;
        public Guid AssignedByUserId { get; set; }
        public string AssignedBy { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewComment { get; set; }
        public List<TaskProgressDto> ProgressUpdates { get; set; } = [];
    }

    public record TaskProgressDto
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public record CreateWorkTaskRequest
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public TaskPriority Priority { get; set; }

        [Required]
        public Guid AssignedEmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }
    }

    public record AddTaskProgressRequest
    {
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }

    public record ReviewWorkTaskRequest
    {
        [Required]
        public bool Accept { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }
}
