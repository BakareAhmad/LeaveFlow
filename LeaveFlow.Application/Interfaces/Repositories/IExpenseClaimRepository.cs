using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IExpenseClaimRepository
    {
        Task AddAsync(ExpenseClaim claim);
        Task<ExpenseClaim?> GetByIdAsync(Guid id);
        Task<List<ExpenseClaim>> GetByEmployeeAsync(Guid employeeId);
        Task<List<ExpenseClaim>> GetForAdminReviewAsync();
        Task<List<ExpenseClaim>> GetAdminClaimHistoryAsync();
        Task AddItemAsync(ExpenseItem item);
        Task<ExpenseItem?> GetItemByIdAsync(Guid itemId);
        void DeleteItem(ExpenseItem item);
        Task AddAttachmentAsync(ExpenseAttachment attachment);
        Task<ExpenseAttachment?> GetAttachmentAsync(Guid attachmentId);
        Task RecalculateTotalAsync(Guid claimId);
        void Update(ExpenseClaim claim);
    }
}
