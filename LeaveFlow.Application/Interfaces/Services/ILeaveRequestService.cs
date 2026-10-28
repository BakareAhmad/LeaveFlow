using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface ILeaveRequestService
    {
        Task<BaseResponse<LeaveRequestDto>> ApplyAsync(
            Guid employeeId,
            ApplyLeaveRequest request);

        Task<BaseResponse<LeaveRequestDto>> UpdateAsync(
            Guid id,
            Guid employeeId,
            UpdateLeaveRequest request);

        Task<BaseResponse<bool>> CancelAsync(Guid id, Guid employeeId);

        Task<BaseResponse<LeaveRequestDto>> ReviewAsync(
            Guid id,
            Guid reviewerUserId,
            ReviewLeaveRequest request);

        Task<BaseResponse<LeaveRequestDto>> GetByIdAsync(Guid id);

        Task<BaseResponse<List<LeaveRequestDto>>> GetByEmployeeIdAsync(Guid employeeId);

        Task<BaseResponse<List<LeaveRequestDto>>> GetReviewableAsync(Guid reviewerUserId);

        Task<BaseResponse<LeaveRequestDto>> GetReviewableByIdAsync(
            Guid id,
            Guid reviewerUserId);
    }
}
