using LeaveFlow.Application.BaseResponse;
using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;

namespace LeaveFlow.Application.Services
{
    public class LeaveAccrualService(
        ILeaveAccrualSettingRepository accrualSettingRepository,
        IEmployeeRepository employeeRepository,
        INotificationService notifications,
        IAuditService audit,
        IUnitOfWork unitOfWork) : ILeaveAccrualService
    {
        private static LeaveAccrualSettingDto MapToDto(LeaveAccrualSetting setting)
        {
            return new LeaveAccrualSettingDto
            {
                Year = setting.Year,
                AccrualAmount = setting.AccrualAmount,
                IsProcessed = setting.IsProcessed,
                ProcessedAt = setting.ProcessedAt
            };
        }

        public async Task<BaseResponse<LeaveAccrualOverviewDto>> GetOverviewAsync()
        {
            var currentYear = DateTime.UtcNow.Year;
            var currentSetting = await accrualSettingRepository.GetByYearAsync(currentYear);
            var nextSetting = await accrualSettingRepository.GetByYearAsync(currentYear + 1);

            return new BaseResponse<LeaveAccrualOverviewDto>
            {
                Status = true,
                Message = "Leave accrual settings retrieved successfully.",
                Data = new LeaveAccrualOverviewDto
                {
                    CurrentYear = currentYear,
                    CurrentYearSetting = currentSetting is null ? null : MapToDto(currentSetting),
                    NextYear = currentYear + 1,
                    NextYearSetting = nextSetting is null ? null : MapToDto(nextSetting)
                }
            };
        }

        public async Task<BaseResponse<LeaveAccrualSettingDto>> ConfigureNextYearAsync(
            UpdateNextYearAccrualRequest request)
        {
            if (request.AccrualAmount < 1 || request.AccrualAmount > 365)
            {
                return new BaseResponse<LeaveAccrualSettingDto>
                {
                    Message = "Annual accrual amount must be between 1 and 365 days."
                };
            }

            var nextYear = DateTime.UtcNow.Year + 1;
            var setting = await accrualSettingRepository.GetByYearAsync(nextYear);

            if (setting is null)
            {
                setting = new LeaveAccrualSetting
                {
                    Year = nextYear,
                    AccrualAmount = request.AccrualAmount,
                    IsProcessed = false
                };

                await accrualSettingRepository.AddAsync(setting);
            }
            else
            {
                if (setting.IsProcessed)
                {
                    return new BaseResponse<LeaveAccrualSettingDto>
                    {
                        Message = "The accrual for this year has already been processed."
                    };
                }

                setting.AccrualAmount = request.AccrualAmount;
                accrualSettingRepository.Update(setting);
            }

            await unitOfWork.SaveChangesAsync();
            await audit.RecordAsync(SystemIds.AdminUserId, "Configured yearly accrual", nameof(LeaveAccrualSetting), setting.Id, $"{nextYear}: {request.AccrualAmount} days");

            return new BaseResponse<LeaveAccrualSettingDto>
            {
                Status = true,
                Message = $"The {nextYear} accrual amount has been set to {request.AccrualAmount} days.",
                Data = MapToDto(setting)
            };
        }

        public async Task<BaseResponse<int>> ProcessCurrentYearAsync()
        {
            var currentYear = DateTime.UtcNow.Year;
            var setting = await accrualSettingRepository.GetByYearAsync(currentYear);

            if (setting is null)
            {
                return new BaseResponse<int>
                {
                    Message = $"No leave accrual amount has been configured for {currentYear}."
                };
            }

            if (setting.IsProcessed)
            {
                return new BaseResponse<int>
                {
                    Message = $"The {currentYear} leave accrual has already been processed."
                };
            }

            var employees = await employeeRepository.GetAllAsync();

            foreach (var employee in employees)
            {
                employee.LeaveBalance += setting.AccrualAmount;
                employeeRepository.Update(employee);
            }

            setting.IsProcessed = true;
            setting.ProcessedAt = DateTime.UtcNow;
            accrualSettingRepository.Update(setting);

            await unitOfWork.SaveChangesAsync();
            await notifications.CreateManyAsync(employees.Select(e => e.UserId), "Yearly leave accrual", $"{setting.AccrualAmount} days added to your balance.", NotificationType.Leave, "/Home/Index");
            await audit.RecordAsync(SystemIds.AdminUserId, "Processed yearly accrual", nameof(LeaveAccrualSetting), setting.Id, $"{employees.Count} employees");

            return new BaseResponse<int>
            {
                Status = true,
                Message = $"Added {setting.AccrualAmount} days to {employees.Count} employee account(s).",
                Data = employees.Count
            };
        }
    }
}
