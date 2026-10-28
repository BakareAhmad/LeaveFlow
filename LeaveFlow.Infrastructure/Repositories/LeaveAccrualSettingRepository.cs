using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class LeaveAccrualSettingRepository(LeaveFlowDbContext context) : ILeaveAccrualSettingRepository
    {
        public async Task AddAsync(LeaveAccrualSetting setting)
        {
            await context.LeaveAccrualSettings.AddAsync(setting);
        }

        public async Task<LeaveAccrualSetting?> GetByYearAsync(int year)
        {
            return await context.LeaveAccrualSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Year == year && !s.IsDeleted);
        }

        public async Task<LeaveAccrualSetting?> GetLatestAsync(int maximumYear)
        {
            return await context.LeaveAccrualSettings
                .AsNoTracking()
                .Where(s => s.Year <= maximumYear && !s.IsDeleted)
                .OrderByDescending(s => s.Year)
                .FirstOrDefaultAsync();
        }

        public void Update(LeaveAccrualSetting setting)
        {
            context.Entry(setting).State = EntityState.Modified;
        }
    }
}
