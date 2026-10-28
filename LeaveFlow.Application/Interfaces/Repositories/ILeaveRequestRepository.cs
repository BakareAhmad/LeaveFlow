using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface ILeaveRequestRepository
    {
        Task AddAsync(LeaveRequest leaveRequest);

        Task<LeaveRequest?> GetByIdAsync(Guid id);

        Task<List<LeaveRequest>> GetForReviewAsync(
            string employeeRoleName,
            Guid? departmentId = null);

        Task<List<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId);

        Task<List<LeaveRequest>> GetByStatusAsync(LeaveStatus status);

        Task<bool> HasOverlappingRequestAsync(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludedRequestId = null);

        void Update(LeaveRequest leaveRequest);
    }
}
