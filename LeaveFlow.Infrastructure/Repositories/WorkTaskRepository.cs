using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace LeaveFlow.Persistence.Repositories
{
    public class WorkTaskRepository(LeaveFlowDbContext context) : IWorkTaskRepository
    {
        private IQueryable<WorkTask> DetailedQuery()
        {
            return context.WorkTasks
                .Include(t => t.Department)
                .Include(t => t.AssignedEmployee).ThenInclude(e => e.User).ThenInclude(u => u.Role)
                .Include(t => t.AssignedByUser)
                .Include(t => t.ProgressUpdates).ThenInclude(p => p.Employee)
                .AsNoTracking()
                .Where(t => !t.IsDeleted);
        }

        public async Task AddAsync(WorkTask task)
        {
            await context.WorkTasks.AddAsync(task);
        }

        public async Task<WorkTask?> GetByIdAsync(Guid id)
        {
            return await DetailedQuery().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<WorkTask>> GetAssignedToEmployeeAsync(Guid employeeId)
        {
            return await DetailedQuery()
                .Where(t => t.AssignedEmployeeId == employeeId)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<List<WorkTask>> GetDepartmentTasksAsync(Guid departmentId)
        {
            return await DetailedQuery()
                .Where(t =>
                    t.DepartmentId == departmentId
                    && t.AssignedEmployee.User.RoleId == SystemIds.EmployeeRoleId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WorkTask>> GetManagerTasksAsync()
        {
            return await DetailedQuery()
                .Where(t => t.AssignedEmployee.User.RoleId == SystemIds.ManagerRoleId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WorkTask>> GetDueSoonAsync(DateTime dueBefore)
        {
            return await DetailedQuery()
                .Where(t =>
                    t.DueDate <= dueBefore
                    && (t.Status == WorkTaskStatus.Assigned
                        || t.Status == WorkTaskStatus.InProgress
                        || t.Status == WorkTaskStatus.Reopened))
                .ToListAsync();
        }

        public async Task AddProgressAsync(TaskProgressUpdate update)
        {
            await context.TaskProgressUpdates.AddAsync(update);
        }

        public async Task<(int assigned, int accepted, int overdue)> GetEmployeeStatsAsync(
            Guid employeeId,
            DateTime start,
            DateTime end)
        {
            var query = context.WorkTasks
                .AsNoTracking()
                .Where(t =>
                    t.AssignedEmployeeId == employeeId
                    && t.CreatedAt >= start
                    && t.CreatedAt <= end
                    && !t.IsDeleted);

            var assigned = await query.CountAsync();
            var accepted = await query.CountAsync(t => t.Status == WorkTaskStatus.Accepted);
            var overdue = await query.CountAsync(t =>
                t.DueDate < DateTime.UtcNow
                && t.Status != WorkTaskStatus.Accepted);

            return (assigned, accepted, overdue);
        }

        public void Update(WorkTask task)
        {
            context.Entry(task).State = EntityState.Modified;
        }
    }
}
