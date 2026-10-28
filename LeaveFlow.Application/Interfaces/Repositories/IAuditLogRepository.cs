using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog auditLog);
        Task<List<AuditLog>> GetRecentAsync(int take = 250);
    }
}
