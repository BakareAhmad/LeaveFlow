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
    public class LeaveRequestRepository(LeaveFlowDbContext context) : ILeaveRequestRepository
    {
        public async Task AddAsync(LeaveRequest leaveRequest)
        {
            await context.LeaveRequests.AddAsync(leaveRequest);
        }

        public async Task<LeaveRequest?> GetByIdAsync(Guid id)
        {
            return await context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e.Department)
                .Include(l => l.Employee).ThenInclude(e => e.User).ThenInclude(u => u.Role)
                .Include(l => l.LeaveType)
                .Include(l => l.Reviewer)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
        }

        public async Task<List<LeaveRequest>> GetForReviewAsync(
            string employeeRoleName,
            Guid? departmentId = null)
        {
            var query = context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e.Department)
                .Include(l => l.Employee).ThenInclude(e => e.User).ThenInclude(u => u.Role)
                .Include(l => l.LeaveType)
                .Include(l => l.Reviewer)
                .AsNoTracking()
                .Where(l =>
                    !l.IsDeleted
                    && l.Employee.User.Role.Name == employeeRoleName);

            if (departmentId.HasValue)
            {
                query = query.Where(l => l.Employee.DepartmentId == departmentId.Value);
            }

            return await query
                .OrderByDescending(l => l.AppliedDate)
                .ToListAsync();
        }

        public async Task<List<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId)
        {
            return await context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e.Department)
                .Include(l => l.LeaveType)
                .Include(l => l.Reviewer)
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId && !l.IsDeleted)
                .OrderByDescending(l => l.AppliedDate)
                .ToListAsync();
        }

        public async Task<List<LeaveRequest>> GetByStatusAsync(LeaveStatus status)
        {
            return await context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e.Department)
                .Include(l => l.Employee).ThenInclude(e => e.User).ThenInclude(u => u.Role)
                .Include(l => l.LeaveType)
                .Include(l => l.Reviewer)
                .AsNoTracking()
                .Where(l => l.Status == status && !l.IsDeleted)
                .OrderByDescending(l => l.AppliedDate)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingRequestAsync(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludedRequestId = null)
        {
            return await context.LeaveRequests
                .AsNoTracking()
                .AnyAsync(l =>
                    l.EmployeeId == employeeId
                    && !l.IsDeleted
                    && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved)
                    && (!excludedRequestId.HasValue || l.Id != excludedRequestId.Value)
                    && l.StartDate <= endDate
                    && l.EndDate >= startDate);
        }

        public void Update(LeaveRequest leaveRequest)
        {
            context.Entry(leaveRequest).State = EntityState.Modified;
        }
    }
}
