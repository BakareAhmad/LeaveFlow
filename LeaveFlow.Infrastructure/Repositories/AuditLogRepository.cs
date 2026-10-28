using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class AuditLogRepository(LeaveFlowDbContext context) : IAuditLogRepository
    {
        public async Task AddAsync(AuditLog auditLog)
        {
            await context.AuditLogs.AddAsync(auditLog);
        }

        public async Task<List<AuditLog>> GetRecentAsync(int take = 250)
        {
            return await context.AuditLogs
                .Include(a => a.User)
                .AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
