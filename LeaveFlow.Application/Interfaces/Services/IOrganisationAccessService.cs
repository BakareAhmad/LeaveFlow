using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface IOrganisationAccessService
    {
        Task<BaseResponse<OrganisationAccessContextDto>> GetContextAsync(Guid userId);
        Task<bool> CanManageEmployeeAsync(Guid userId, Guid employeeId);
    }
}
