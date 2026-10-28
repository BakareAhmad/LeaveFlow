using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class EmployeeRepository(LeaveFlowDbContext context) : IEmployeeRepository
    {
        public async Task AddAsync(Employee employee)
        {
            await context.Employees.AddAsync(employee);
        }

        public async Task<Employee?> GetByIdAsync(Guid id)
        {
            return await context.Employees
                .Include(e => e.User).ThenInclude(u => u.Role)
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        }

        public async Task<Employee?> GetByUserIdAsync(Guid userId)
        {
            return await context.Employees
                .Include(e => e.User).ThenInclude(u => u.Role)
                .Include(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted);
        }

        public async Task<bool> ManagerExistsInDepartmentAsync(
            Guid departmentId,
            Guid? excludedEmployeeId = null)
        {
            return await context.Employees
                .AsNoTracking()
                .AnyAsync(e =>
                    e.DepartmentId == departmentId
                    && e.User.RoleId == SystemIds.ManagerRoleId
                    && !e.IsDeleted
                    && !e.User.IsDeleted
                    && (!excludedEmployeeId.HasValue || e.Id != excludedEmployeeId.Value));
        }

        public async Task<Employee?> GetManagerByDepartmentAsync(Guid departmentId)
        {
            return await context.Employees
                .Include(e => e.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.DepartmentId == departmentId
                    && e.User.RoleId == SystemIds.ManagerRoleId
                    && !e.IsDeleted
                    && !e.User.IsDeleted);
        }

        public async Task<List<Employee>> GetAllAsync()
        {
            return await context.Employees
                .Include(e => e.User).ThenInclude(u => u.Role)
                .Include(e => e.Department)
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .AsNoTracking()
                .ToListAsync();
        }

        public void Update(Employee employee)
        {
            context.Entry(employee).State = EntityState.Modified;
        }
    }
}
