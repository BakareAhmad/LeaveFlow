using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class OperationsReportRepository(LeaveFlowDbContext context) : IOperationsReportRepository
    {
        public async Task<OperationsReportDto> GetAsync(
            Guid? departmentId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var start = startDate?.Date;
            var endExclusive = endDate?.Date.AddDays(1);

            // Employees in scope
            var employees = context.Employees
                .AsNoTracking()
                .Where(e => !e.IsDeleted);
            if (departmentId.HasValue)
            {
                employees = employees.Where(e => e.DepartmentId == departmentId.Value);
            }

            // Leave requests in scope
            var leaves = context.LeaveRequests
                .AsNoTracking()
                .Where(l => !l.IsDeleted);
            if (departmentId.HasValue)
            {
                leaves = leaves.Where(l => l.Employee.DepartmentId == departmentId.Value);
            }
            if (start.HasValue)
            {
                leaves = leaves.Where(l => l.AppliedDate >= start.Value);
            }
            if (endExclusive.HasValue)
            {
                leaves = leaves.Where(l => l.AppliedDate < endExclusive.Value);
            }

            // Tasks in scope
            var tasks = context.WorkTasks
                .AsNoTracking()
                .Where(t => !t.IsDeleted);
            if (departmentId.HasValue)
            {
                tasks = tasks.Where(t => t.DepartmentId == departmentId.Value);
            }
            if (start.HasValue)
            {
                tasks = tasks.Where(t => t.CreatedAt >= start.Value);
            }
            if (endExclusive.HasValue)
            {
                tasks = tasks.Where(t => t.CreatedAt < endExclusive.Value);
            }

            // Expense claims in scope
            var expenses = context.ExpenseClaims
                .AsNoTracking()
                .Where(c => !c.IsDeleted);
            if (departmentId.HasValue)
            {
                expenses = expenses.Where(c => c.DepartmentId == departmentId.Value);
            }
            if (start.HasValue)
            {
                expenses = expenses.Where(c => c.CreatedAt >= start.Value);
            }
            if (endExclusive.HasValue)
            {
                expenses = expenses.Where(c => c.CreatedAt < endExclusive.Value);
            }

            // Performance reviews in scope
            var reviews = context.PerformanceReviews
                .AsNoTracking()
                .Where(r => !r.IsDeleted);
            if (departmentId.HasValue)
            {
                reviews = reviews.Where(r => r.Employee.DepartmentId == departmentId.Value);
            }
            if (start.HasValue)
            {
                reviews = reviews.Where(r => r.Cycle.StartDate >= start.Value);
            }
            if (endExclusive.HasValue)
            {
                reviews = reviews.Where(r => r.Cycle.StartDate < endExclusive.Value);
            }

            var dto = new OperationsReportDto
            {
                StartDate = start,
                EndDate = endDate?.Date,
                Employees = await employees.CountAsync(),
                PendingLeave = await leaves.CountAsync(l => l.Status == LeaveStatus.Pending),
                OpenTasks = await tasks.CountAsync(t => t.Status != WorkTaskStatus.Accepted),
                OverdueTasks = await tasks.CountAsync(t =>
                    t.DueDate < DateTime.UtcNow
                    && t.Status != WorkTaskStatus.Accepted),
                PendingExpenses = await expenses.CountAsync(c =>
                    c.Status == ExpenseClaimStatus.Submitted),
                ApprovedExpenseTotal = await expenses
                    .Where(c => c.Status == ExpenseClaimStatus.AdminApproved)
                    .SumAsync(c => (decimal?)c.TotalAmount) ?? 0,
                OpenReviewCycles = await context.PerformanceReviewCycles
                    .AsNoTracking()
                    .CountAsync(c => c.IsOpen && !c.IsDeleted),
                PendingPerformanceReviews = await reviews.CountAsync(r =>
                    r.Status != PerformanceReviewStatus.Submitted)
            };

            return dto;
        }
    }
}
