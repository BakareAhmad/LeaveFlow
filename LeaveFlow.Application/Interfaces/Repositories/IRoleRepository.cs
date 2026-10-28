using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);

        Task<List<Role>> GetAllAsync();
    }
}
