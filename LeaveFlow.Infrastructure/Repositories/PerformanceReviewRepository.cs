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
    public class PerformanceReviewRepository(LeaveFlowDbContext context) : IPerformanceReviewRepository
    {
        private IQueryable<PerformanceReview> DetailedQuery()
        {
            return context.PerformanceReviews
                .Include(r => r.Cycle)
                .Include(r => r.Employee).ThenInclude(e => e.User).ThenInclude(u => u.Role)
                .Include(r => r.Employee).ThenInclude(e => e.Department)
                .Include(r => r.Reviewer).ThenInclude(u => u!.Employee)
                .AsNoTracking()
                .Where(r => !r.IsDeleted);
        }

        public async Task AddCycleAsync(PerformanceReviewCycle cycle)
        {
            await context.PerformanceReviewCycles.AddAsync(cycle);
        }

        public async Task<PerformanceReviewCycle?> GetCycleAsync(Guid id)
        {
            return await context.PerformanceReviewCycles
                .Include(c => c.Reviews)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<List<PerformanceReviewCycle>> GetCyclesAsync()
        {
            return await context.PerformanceReviewCycles
                .Include(c => c.Reviews)
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }

        public async Task AddReviewAsync(PerformanceReview review)
        {
            await context.PerformanceReviews.AddAsync(review);
        }

        public async Task<PerformanceReview?> GetReviewAsync(Guid id)
        {
            return await DetailedQuery().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PerformanceReview?> GetReviewByCycleAndEmployeeAsync(
            Guid cycleId,
            Guid employeeId)
        {
            return await DetailedQuery()
                .FirstOrDefaultAsync(r => r.CycleId == cycleId && r.EmployeeId == employeeId);
        }

        public async Task<List<PerformanceReview>> GetEmployeeReviewsAsync(Guid employeeId)
        {
            return await DetailedQuery()
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.Cycle.StartDate)
                .ToListAsync();
        }

        public async Task<List<PerformanceReview>> GetDepartmentReviewsAsync(
            Guid cycleId,
            Guid departmentId)
        {
            return await DetailedQuery()
                .Where(r =>
                    r.CycleId == cycleId
                    && r.Employee.DepartmentId == departmentId
                    && r.Employee.User.RoleId == SystemIds.EmployeeRoleId)
                .OrderBy(r => r.Employee.FirstName)
                .ThenBy(r => r.Employee.LastName)
                .ToListAsync();
        }

        public async Task<List<PerformanceReview>> GetReviewsByCycleAsync(Guid cycleId)
        {
            return await DetailedQuery()
                .Where(r => r.CycleId == cycleId)
                .OrderBy(r => r.Employee.Department.Name)
                .ThenBy(r => r.Employee.FirstName)
                .ToListAsync();
        }

        public async Task<bool> ReviewExistsAsync(Guid cycleId, Guid employeeId)
        {
            return await context.PerformanceReviews
                .AsNoTracking()
                .AnyAsync(r =>
                    r.CycleId == cycleId
                    && r.EmployeeId == employeeId
                    && !r.IsDeleted);
        }

        public async Task<bool> HasOverlappingCycleAsync(
            DateTime startDate,
            DateTime endDate,
            Guid? excludedCycleId = null)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            return await context.PerformanceReviewCycles
                .AsNoTracking()
                .AnyAsync(c =>
                    !c.IsDeleted
                    && (!excludedCycleId.HasValue || c.Id != excludedCycleId.Value)
                    && c.StartDate.Date <= end
                    && c.EndDate.Date >= start);
        }

        public void UpdateCycle(PerformanceReviewCycle cycle)
        {
            context.Entry(cycle).State = EntityState.Modified;
        }

        public void UpdateReview(PerformanceReview review)
        {
            context.Entry(review).State = EntityState.Modified;
        }

        public async Task<DepartmentReviewReport?> GetDepartmentReportAsync(
            Guid cycleId,
            Guid departmentId)
        {
            return await context.DepartmentReviewReports
                .Include(r => r.Cycle)
                .Include(r => r.Department)
                .Include(r => r.ManagerUser).ThenInclude(u => u!.Employee)
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.CycleId == cycleId
                    && r.DepartmentId == departmentId
                    && !r.IsDeleted);
        }

        public async Task<List<DepartmentReviewReport>> GetDepartmentReportsByCycleAsync(Guid cycleId)
        {
            return await context.DepartmentReviewReports
                .Include(r => r.Cycle)
                .Include(r => r.Department)
                .Include(r => r.ManagerUser).ThenInclude(u => u!.Employee)
                .AsNoTracking()
                .Where(r => r.CycleId == cycleId && !r.IsDeleted)
                .OrderBy(r => r.Department.Name)
                .ToListAsync();
        }

        public async Task AddDepartmentReportAsync(DepartmentReviewReport report)
        {
            await context.DepartmentReviewReports.AddAsync(report);
        }

        public void UpdateDepartmentReport(DepartmentReviewReport report)
        {
            context.Entry(report).State = EntityState.Modified;
        }
    }
}
