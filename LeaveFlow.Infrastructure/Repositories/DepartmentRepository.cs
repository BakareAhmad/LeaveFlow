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
    public class DepartmentRepository(LeaveFlowDbContext context) : IDepartmentRepository
    {
        public async Task AddAsync(Department department)
        {
            await context.Departments.AddAsync(department);
        }

        public async Task<Department?> GetByIdAsync(Guid id)
        {
            return await context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        }

        public async Task<List<Department>> GetAllAsync()
        {
            return await context.Departments
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<bool> DepartmentNameExistsAsync(string name)
        {
            name = name.Trim();

            return await context.Departments
                .AsNoTracking()
                .AnyAsync(d => d.Name.Trim().ToLower() == name.ToLower() && !d.IsDeleted);
        }

        public void Update(Department department)
        {
            context.Departments.Update(department);
        }
    }
}
