using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class LeaveAccrualController(ILeaveAccrualService leaveAccrualService)
        : Controller
    {
        private async Task<LeaveAccrualViewModel> BuildViewModelAsync(
            UpdateNextYearAccrualRequest? request = null)
        {
            var response = await leaveAccrualService.GetOverviewAsync();
            var overview = response.Data ?? new LeaveAccrualOverviewDto();

            return new LeaveAccrualViewModel
            {
                Overview = overview,
                NextYearAccrual = request ?? new UpdateNextYearAccrualRequest
                {
                    AccrualAmount = overview.NextYearSetting?.AccrualAmount
                        ?? overview.CurrentYearSetting?.AccrualAmount
                        ?? 30
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildViewModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureNextYear(LeaveAccrualViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(Index), await BuildViewModelAsync(model.NextYearAccrual));
            }

            var response = await leaveAccrualService.ConfigureNextYearAsync(
                model.NextYearAccrual);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(nameof(Index), await BuildViewModelAsync(model.NextYearAccrual));
            }

            TempData["Success"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCurrentYear()
        {
            var response = await leaveAccrualService.ProcessCurrentYearAsync();

            TempData[response.Status ? "Success" : "Error"] = response.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
