using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface ILeaveTypeRepository
    {
        Task AddAsync(LeaveType leaveType);

        Task<LeaveType?> GetByIdAsync(Guid id);

        Task<List<LeaveType>> GetAllAsync();

        Task<bool> LeaveTypeNameExistsAsync(string name);

        void Update(LeaveType leaveType);
    }
}
