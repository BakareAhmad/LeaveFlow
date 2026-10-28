using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<BaseResponse<RoleDto>> GetByIdAsync(Guid id);

        Task<BaseResponse<List<RoleDto>>> GetAllAsync();
    }
}
