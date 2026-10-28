using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class LeaveTypeRepository(LeaveFlowDbContext context) : ILeaveTypeRepository
    {
        public async Task AddAsync(LeaveType leaveType)
        {
            await context.LeaveTypes.AddAsync(leaveType);
        }

        public async Task<LeaveType?> GetByIdAsync(Guid id)
        {
            return await context.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(lt => lt.Id == id && !lt.IsDeleted);
        }

        public async Task<List<LeaveType>> GetAllAsync()
        {
            return await context.LeaveTypes
                .AsNoTracking()
                .Where(lt => !lt.IsDeleted)
                .OrderBy(lt => lt.Name)
                .ToListAsync();
        }

        public async Task<bool> LeaveTypeNameExistsAsync(string name)
        {
            name = name.Trim();

            return await context.LeaveTypes
                .AsNoTracking()
                .AnyAsync(lt =>
                    lt.Name.Trim().ToLower() == name.ToLower()
                    && !lt.IsDeleted);
        }

        public void Update(LeaveType leaveType)
        {
            context.LeaveTypes.Update(leaveType);
        }
    }
}
