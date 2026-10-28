using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IWorkTaskService
    {
        Task<BaseResponse<WorkTaskDto>> CreateAsync(Guid assignerUserId, CreateWorkTaskRequest request);
        Task<BaseResponse<List<WorkTaskDto>>> GetMyTasksAsync(Guid employeeId);
        Task<BaseResponse<List<WorkTaskDto>>> GetReviewQueueAsync(Guid reviewerUserId);
        Task<BaseResponse<WorkTaskDto>> GetByIdAsync(Guid id, Guid userId);
        Task<BaseResponse<WorkTaskDto>> StartAsync(Guid id, Guid employeeId);
        Task<BaseResponse<WorkTaskDto>> AddProgressAsync(Guid id, Guid employeeId, AddTaskProgressRequest request);
        Task<BaseResponse<WorkTaskDto>> CompleteAsync(Guid id, Guid employeeId);
        Task<BaseResponse<WorkTaskDto>> ReviewAsync(Guid id, Guid reviewerUserId, ReviewWorkTaskRequest request);
    }
}
