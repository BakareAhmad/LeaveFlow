using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using Mapster;

namespace LeaveFlow.Application.Services
{
    public class RoleService(IRoleRepository roleRepository) : IRoleService
    {
        public async Task<BaseResponse<RoleDto>> GetByIdAsync(Guid id)
        {
            var role = await roleRepository.GetByIdAsync(id);

            if (role is null)
            {
                return new BaseResponse<RoleDto>
                {
                    Message = "Role not found."
                };
            }

            return new BaseResponse<RoleDto>
            {
                Status = true,
                Message = "Role retrieved successfully.",
                Data = role.Adapt<RoleDto>()
            };
        }

        public async Task<BaseResponse<List<RoleDto>>> GetAllAsync()
        {
            var roles = await roleRepository.GetAllAsync();

            return new BaseResponse<List<RoleDto>>
            {
                Status = true,
                Message = "Roles retrieved successfully.",
                Data = roles.Adapt<List<RoleDto>>()
            };
        }
    }
}
