using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IEmployeeRepository
    {
        Task AddAsync(Employee employee);

        void Update(Employee employee);

        Task<Employee?> GetByIdAsync(Guid id);

        Task<Employee?> GetByUserIdAsync(Guid userId);

        Task<bool> ManagerExistsInDepartmentAsync(
            Guid departmentId,
            Guid? excludedEmployeeId = null);

        Task<Employee?> GetManagerByDepartmentAsync(Guid departmentId);

        Task<List<Employee>> GetAllAsync();
    }
}
