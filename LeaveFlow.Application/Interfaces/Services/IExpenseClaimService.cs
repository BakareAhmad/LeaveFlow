using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IExpenseClaimService
    {
        Task<BaseResponse<ExpenseClaimDto>> CreateDraftAsync(Guid employeeId, CreateExpenseClaimRequest request);
        Task<BaseResponse<List<ExpenseClaimDto>>> GetMyClaimsAsync(Guid employeeId);
        Task<BaseResponse<List<ExpenseClaimDto>>> GetReviewQueueAsync(Guid reviewerUserId);
        Task<BaseResponse<List<ExpenseClaimDto>>> GetClaimHistoryAsync(Guid userId);
        Task<BaseResponse<ExpenseClaimDto>> GetByIdAsync(Guid id, Guid userId);
        Task<BaseResponse<ExpenseClaimDto>> AddItemAsync(Guid id, Guid employeeId, AddExpenseItemRequest request);
        Task<BaseResponse<bool>> DeleteItemAsync(Guid id, Guid itemId, Guid employeeId);
        Task<BaseResponse<ExpenseClaimDto>> AddAttachmentAsync(Guid id, Guid employeeId, AddExpenseAttachmentRequest request);
        Task<BaseResponse<ExpenseClaimDto>> SubmitAsync(Guid id, Guid employeeId);
        Task<BaseResponse<ExpenseClaimDto>> ReviewAsync(Guid id, Guid reviewerUserId, ReviewExpenseClaimRequest request);
        Task<BaseResponse<ExpenseAttachmentDto>> GetAttachmentAsync(Guid attachmentId, Guid userId);
    }
}
