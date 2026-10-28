using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface ILeaveAccrualSettingRepository
    {
        Task AddAsync(LeaveAccrualSetting setting);

        Task<LeaveAccrualSetting?> GetByYearAsync(int year);

        Task<LeaveAccrualSetting?> GetLatestAsync(int maximumYear);

        void Update(LeaveAccrualSetting setting);
    }
}
