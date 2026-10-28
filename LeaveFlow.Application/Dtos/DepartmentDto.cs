using System;
using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Application.Dtos
{
    public record DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public record CreateDepartmentRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
    }

    public record UpdateDepartmentRequest
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
