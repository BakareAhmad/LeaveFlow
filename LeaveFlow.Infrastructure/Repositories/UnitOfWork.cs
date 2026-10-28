using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Persistence.Context;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class UnitOfWork(LeaveFlowDbContext context) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync();
        }
    }
}
