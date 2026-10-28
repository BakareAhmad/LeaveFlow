using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class OrganisationAccessService(IUserRepository userRepository,
            IEmployeeRepository employeeRepository) : IOrganisationAccessService
    {
        public async Task<BaseResponse<OrganisationAccessContextDto>> GetContextAsync(Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<OrganisationAccessContextDto>
                {
                    Message = "User not found."
                };
            }

            var dto = new OrganisationAccessContextDto
            {
                UserId = user.Id,
                EmployeeId = user.Employee.Id,
                DepartmentId = user.Employee.DepartmentId,
                RoleName = user.Role.Name,
                IsSystemAdmin = user.Id == SystemIds.AdminUserId
            };

            return new BaseResponse<OrganisationAccessContextDto>
            {
                Status = true,
                Data = dto
            };
        }

        public async Task<bool> CanManageEmployeeAsync(Guid userId, Guid employeeId)
        {
            var actor = await userRepository.GetByIdAsync(userId);
            var target = await employeeRepository.GetByIdAsync(employeeId);

            if (actor == null || target == null)
            {
                return false;
            }

            // System Admin can review managers
            if (actor.Id == SystemIds.AdminUserId)
            {
                return target.User.Role.Name == RoleNames.Manager;
            }

            // Department Manager can review employees within their own department (but not themselves)
            if (actor.Role.Name == RoleNames.Manager
                && target.User.Role.Name == RoleNames.Employee
                && actor.Employee.DepartmentId == target.DepartmentId
                && actor.Employee.Id != target.Id)
            {
                return true;
            }

            return false;
        }
    }
}
