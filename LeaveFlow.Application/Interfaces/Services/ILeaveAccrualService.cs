using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Services
{
    public interface ILeaveAccrualService
    {
        Task<BaseResponse<LeaveAccrualOverviewDto>> GetOverviewAsync();

        Task<BaseResponse<LeaveAccrualSettingDto>> ConfigureNextYearAsync(
            UpdateNextYearAccrualRequest request);

        Task<BaseResponse<int>> ProcessCurrentYearAsync();
    }
}
