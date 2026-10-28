using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IDepartmentRepository
    {
        Task AddAsync(Department department);

        Task<Department?> GetByIdAsync(Guid id);

        Task<List<Department>> GetAllAsync();

        Task<bool> DepartmentNameExistsAsync(string name);

        void Update(Department department);
    }
}
