using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IWorkTaskRepository
    {
        Task AddAsync(WorkTask task);
        Task<WorkTask?> GetByIdAsync(Guid id);
        Task<List<WorkTask>> GetAssignedToEmployeeAsync(Guid employeeId);
        Task<List<WorkTask>> GetDepartmentTasksAsync(Guid departmentId);
        Task<List<WorkTask>> GetManagerTasksAsync();
        Task<List<WorkTask>> GetDueSoonAsync(DateTime dueBefore);
        Task AddProgressAsync(TaskProgressUpdate update);
        Task<(int assigned, int accepted, int overdue)> GetEmployeeStatsAsync(Guid employeeId, DateTime start, DateTime end);
        void Update(WorkTask task);
    }
}
