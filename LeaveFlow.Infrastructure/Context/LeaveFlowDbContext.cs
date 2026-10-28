using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveFlow.Persistence.Context
{
    public class LeaveFlowDbContext(DbContextOptions<LeaveFlowDbContext> options)
    : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<LeaveAccrualSetting> LeaveAccrualSettings => Set<LeaveAccrualSetting>();
        public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
        public DbSet<TaskProgressUpdate> TaskProgressUpdates => Set<TaskProgressUpdate>();
        public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
        public DbSet<ExpenseItem> ExpenseItems => Set<ExpenseItem>();
        public DbSet<ExpenseAttachment> ExpenseAttachments => Set<ExpenseAttachment>();
        public DbSet<PerformanceReviewCycle> PerformanceReviewCycles => Set<PerformanceReviewCycle>();
        public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
        public DbSet<DepartmentReviewReport> DepartmentReviewReports => Set<DepartmentReviewReport>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new
                {
                    Id = SystemIds.AdminRoleId,
                    Name = RoleNames.Admin,
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                },
                new
                {
                    Id = SystemIds.ManagerRoleId,
                    Name = RoleNames.Manager,
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                },
                new
                {
                    Id = SystemIds.EmployeeRoleId,
                    Name = RoleNames.Employee,
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                });

            modelBuilder.Entity<Department>().HasData(
                new
                {
                    Id = SystemIds.AdministrationDepartmentId,
                    Name = "Administration",
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                });

            modelBuilder.Entity<User>().HasData(
                new
                {
                    Id = SystemIds.AdminUserId,
                    Email = "admin@leaveflow.com",
                    PasswordHash = "$2a$11$AqbhDz26NQN0Yowwsnt7teOd/GhUoXiE7G9QqZJw8BaTAy.5TqXXi",
                    RoleId = SystemIds.AdminRoleId,
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                });

            modelBuilder.Entity<Employee>().HasData(
                new
                {
                    Id = SystemIds.AdminEmployeeId,
                    FirstName = "System",
                    LastName = "Administrator",
                    PhoneNumber = "00000000000",
                    DepartmentId = SystemIds.AdministrationDepartmentId,
                    LeaveBalance = 30,
                    UserId = SystemIds.AdminUserId,
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                });

            modelBuilder.Entity<LeaveType>().HasData(
                new
                {
                    Id = SystemIds.AnnualLeaveTypeId,
                    Name = "Annual Leave",
                    MaximumDays = 30,
                    Description = "Standard annual paid leave entitlement.",
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                });

            modelBuilder.Entity<LeaveAccrualSetting>().HasData(
                new
                {
                    Id = SystemIds.CurrentAccrualSettingId,
                    Year = 2026,
                    AccrualAmount = 30,
                    IsProcessed = true,
                    ProcessedAt = (DateTime?)new DateTime(
                        2026,
                        1,
                        1,
                        0,
                        0,
                        0,
                        DateTimeKind.Utc),
                    IsDeleted = false,
                    DeletedAt = (DateTime?)null
                });

            modelBuilder.Entity<LeaveAccrualSetting>()
                .HasIndex(s => s.Year)
                .IsUnique();

            modelBuilder.Entity<Department>()
                .HasMany(d => d.Employees)
                .WithOne(e => e.Department)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Reviewer)
                .WithMany()
                .HasForeignKey(lr => lr.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .Property(t => t.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.AssignedEmployee)
                .WithMany()
                .HasForeignKey(t => t.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.AssignedByUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskProgressUpdate>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskProgressUpdate>()
                .HasOne(p => p.WorkTask)
                .WithMany(t => t.ProgressUpdates)
                .HasForeignKey(p => p.WorkTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExpenseClaim>()
                .Property(c => c.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ExpenseClaim>()
                .Property(c => c.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<ExpenseItem>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ExpenseClaim>()
                .HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseClaim>()
                .HasOne(c => c.Department)
                .WithMany()
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseClaim>()
                .HasOne(c => c.AdminReviewer)
                .WithMany()
                .HasForeignKey(c => c.AdminReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseClaim>()
                .HasOne(c => c.WorkTask)
                .WithMany()
                .HasForeignKey(c => c.WorkTaskId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExpenseItem>()
                .HasOne(i => i.ExpenseClaim)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.ExpenseClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExpenseAttachment>()
                .HasOne(a => a.ExpenseClaim)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.ExpenseClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PerformanceReview>()
                .HasIndex(r => new { r.CycleId, r.EmployeeId })
                .IsUnique();

            modelBuilder.Entity<PerformanceReview>()
                .Property(r => r.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DepartmentReviewReport>()
                .HasIndex(r => new { r.CycleId, r.DepartmentId })
                .IsUnique();

            modelBuilder.Entity<DepartmentReviewReport>()
                .HasOne(r => r.Cycle)
                .WithMany()
                .HasForeignKey(r => r.CycleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DepartmentReviewReport>()
                .HasOne(r => r.Department)
                .WithMany()
                .HasForeignKey(r => r.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DepartmentReviewReport>()
                .HasOne(r => r.ManagerUser)
                .WithMany()
                .HasForeignKey(r => r.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.DeduplicationKey)
                .IsUnique()
                .HasFilter("[DeduplicationKey] IS NOT NULL");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);
        }
    }
}
