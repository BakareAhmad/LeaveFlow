using LeaveFlow.Application.Dtos;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IOperationsReportRepository
    {
        Task<OperationsReportDto> GetAsync(Guid? departmentId = null, DateTime? startDate = null, DateTime? endDate = null);
    }
}
