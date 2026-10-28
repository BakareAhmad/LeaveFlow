using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Domain.Entities;
using LeaveFlow.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Repositories
{
    public class UserRepository(LeaveFlowDbContext context) : IUserRepository
    {
        public async Task AddAsync(User user)
        {
            await context.Users.AddAsync(user);
        }

        public void Update(User user)
        {
            context.Entry(user).State = EntityState.Modified;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await context.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            email = email.Trim().ToLower();

            return await context.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email && !u.IsDeleted);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await context.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                    .ThenInclude(e => e.Department)
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.Email)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            email = email.Trim().ToLower();

            return await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == email && !u.IsDeleted);
        }
    }
}
