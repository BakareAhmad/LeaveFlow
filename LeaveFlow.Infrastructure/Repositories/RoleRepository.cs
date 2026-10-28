using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class RoleRepository(LeaveFlowDbContext context) : IRoleRepository
    {
        public async Task<Role?> GetByIdAsync(Guid id)
        {
            return await context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await context.Roles
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}
