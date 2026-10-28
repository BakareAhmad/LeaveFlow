using System;

namespace LeaveFlow.Application.Dtos
{
    public record RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
