using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record ExpenseClaimDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string EmployeeRole { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
        public ExpenseClaimStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? AdminComment { get; set; }
        public Guid? WorkTaskId { get; set; }
        public List<ExpenseItemDto> Items { get; set; } = [];
        public List<ExpenseAttachmentDto> Attachments { get; set; } = [];
    }

    public record ExpenseItemDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public record ExpenseAttachmentDto
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public record CreateExpenseClaimRequest
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public Guid? WorkTaskId { get; set; }
    }

    public record AddExpenseItemRequest
    {
        [Required]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
    }

    public record ReviewExpenseClaimRequest
    {
        public bool Approve { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public record AddExpenseAttachmentRequest
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
