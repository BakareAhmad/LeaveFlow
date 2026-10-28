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
    public class ExpenseClaimRepository(LeaveFlowDbContext context) : IExpenseClaimRepository
    {
        private IQueryable<ExpenseClaim> DetailedQuery()
        {
            return context.ExpenseClaims
                .Include(c => c.Employee).ThenInclude(e => e.User).ThenInclude(u => u.Role)
                .Include(c => c.Employee).ThenInclude(e => e.Department)
                .Include(c => c.Department)
                .Include(c => c.WorkTask)
                .Include(c => c.Items)
                .Include(c => c.Attachments)
                .AsNoTracking()
                .Where(c => !c.IsDeleted);
        }

        public async Task AddAsync(ExpenseClaim claim)
        {
            await context.ExpenseClaims.AddAsync(claim);
        }

        public async Task<ExpenseClaim?> GetByIdAsync(Guid id)
        {
            return await DetailedQuery().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<ExpenseClaim>> GetByEmployeeAsync(Guid employeeId)
        {
            return await DetailedQuery()
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ExpenseClaim>> GetForAdminReviewAsync()
        {
            return await DetailedQuery()
                .Where(c => c.Status == ExpenseClaimStatus.Submitted)
                .OrderBy(c => c.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<ExpenseClaim>> GetAdminClaimHistoryAsync()
        {
            return await DetailedQuery()
                .Where(c =>
                    c.Status == ExpenseClaimStatus.AdminApproved
                    || c.Status == ExpenseClaimStatus.AdminRejected)
                .OrderByDescending(c => c.AdminReviewedAt ?? c.SubmittedAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task AddItemAsync(ExpenseItem item)
        {
            await context.ExpenseItems.AddAsync(item);
        }

        public async Task<ExpenseItem?> GetItemByIdAsync(Guid itemId)
        {
            return await context.ExpenseItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted);
        }

        public void DeleteItem(ExpenseItem item)
        {
            context.ExpenseItems.Remove(item);
        }

        public async Task AddAttachmentAsync(ExpenseAttachment attachment)
        {
            await context.ExpenseAttachments.AddAsync(attachment);
        }

        public async Task<ExpenseAttachment?> GetAttachmentAsync(Guid attachmentId)
        {
            return await context.ExpenseAttachments
                .Include(a => a.ExpenseClaim)
                    .ThenInclude(c => c.Employee)
                    .ThenInclude(e => e.User)
                    .ThenInclude(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);
        }

        public async Task RecalculateTotalAsync(Guid claimId)
        {
            var total = await context.ExpenseItems
                .Where(i => i.ExpenseClaimId == claimId && !i.IsDeleted)
                .SumAsync(i => (decimal?)i.Amount) ?? 0;

            var claim = await context.ExpenseClaims.FirstAsync(c => c.Id == claimId);
            claim.TotalAmount = total;
        }

        public void Update(ExpenseClaim claim)
        {
            context.Entry(claim).State = EntityState.Modified;
        }
    }
}
