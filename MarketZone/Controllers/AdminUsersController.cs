using System.Security.Claims;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketZone.Controllers
{
	[Authorize(Policy = "AdminOnly")]
	public class AdminUsersController : Controller
	{
		private readonly IUserService userService;

		public AdminUsersController(IUserService userService)
		{
			this.userService = userService;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string? search, int page = 1)
		{
			var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var model = await userService.GetAdminUsersPageAsync(search, page, adminId!);
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(string id, string? returnUrl)
		{
			var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var success = await userService.SoftDeleteUserAsync(id, adminId);

			TempData["StatusMessage"] = success
				? "User deleted successfully."
				: "Cannot delete this user.";

			if (!string.IsNullOrWhiteSpace(returnUrl))
				return LocalRedirect(returnUrl);

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> PromoteToModerator(string id, string? returnUrl)
		{
			var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var ok = await userService.PromoteToModeratorAsync(id, adminId);

			TempData["StatusMessage"] = ok
				? "User promoted to Moderator."
				: "Cannot promote this user.";

			if (!string.IsNullOrWhiteSpace(returnUrl))
				return LocalRedirect(returnUrl);

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DemoteFromModerator(string id, string? returnUrl)
		{
			var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var ok = await userService.DemoteFromModeratorAsync(id, adminId);

			TempData["StatusMessage"] = ok
				? "Moderator role removed."
				: "Cannot demote this user.";

			if (!string.IsNullOrWhiteSpace(returnUrl))
				return LocalRedirect(returnUrl);

			return RedirectToAction(nameof(Index));
		}
	}
}