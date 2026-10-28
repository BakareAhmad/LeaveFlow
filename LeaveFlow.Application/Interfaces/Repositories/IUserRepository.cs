using LeaveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task AddAsync(User user);

        void Update(User user);

        Task<User?> GetByIdAsync(Guid id);

        Task<User?> GetByEmailAsync(string email);

        Task<List<User>> GetAllAsync();

        Task<bool> EmailExistsAsync(string email);
    }
}
