using LeaveFlow.Application.Interfaces.Repositories;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeaveFlow.Host.Services
{
    public class OperationsReminderWorker(IServiceScopeFactory scopeFactory,
            ILogger<OperationsReminderWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var tasks = scope.ServiceProvider.GetRequiredService<IWorkTaskRepository>();
                    var reviews = scope.ServiceProvider.GetRequiredService<IPerformanceReviewRepository>();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var dueBefore = DateTime.UtcNow.AddDays(1);

                    // Task due reminders
                    var dueSoonTasks = await tasks.GetDueSoonAsync(dueBefore);
                    foreach (var task in dueSoonTasks)
                    {
                        var dedupeKey = $"task-due:{task.Id}:{DateTime.UtcNow:yyyyMMdd}";
                        await notifications.CreateAsync(
                            task.AssignedEmployee.UserId,
                            "Task due soon",
                            task.Title,
                            NotificationType.Reminder,
                            $"/WorkTask/Details/{task.Id}",
                            dedupeKey);
                    }

                    // Performance review deadline reminders
                    var cycles = await reviews.GetCyclesAsync();
                    var upcomingDeadline = DateTime.UtcNow.AddDays(3);

                    foreach (var cycle in cycles.Where(c => c.IsOpen && c.EndDate <= upcomingDeadline))
                    {
                        foreach (var reviewSummary in cycle.Reviews.Where(r => r.Status != PerformanceReviewStatus.Submitted))
                        {
                            var review = await reviews.GetReviewAsync(reviewSummary.Id);
                            if (review != null)
                            {
                                var dedupeKey = $"review-due:{review.Id}:{DateTime.UtcNow:yyyyMMdd}";
                                await notifications.CreateAsync(
                                    review.Employee.UserId,
                                    "Performance review deadline",
                                    cycle.Name,
                                    NotificationType.Reminder,
                                    $"/PerformanceReview/Details/{review.Id}",
                                    dedupeKey);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Operations reminder worker failed.");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}
