using System.Security.Claims;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketZone.Controllers
{
	[Authorize(Roles = "Admin,Moderator")]
	public class ModerationController : Controller
	{
		private readonly IAdService adService;

		public ModerationController(IAdService adService)
		{
			this.adService = adService;
		}

		[HttpGet]
		public async Task<IActionResult> Pending(string? search,string? address,
	    int? categoryId,decimal? minPrice,decimal? maxPrice,string? tags,
	    string? sort,int page = 1)
		{
			var model = await adService.GetPendingAsync(
				search,
				address,
				categoryId,
				minPrice,
				maxPrice,
				tags,
				sort,
				page);

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Approve(int id, string? returnUrl = null, string? search = null, int page = 1)
		{
			var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var ok = await adService.ApproveAsync(id, reviewerId);
			if (!ok) return NotFound();

			TempData["StatusMessage"] = "Ad approved.";

			if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);

			return RedirectToAction(nameof(Pending), new { search, page });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reject(int id, string reason, string? returnUrl = null, string? search = null, int page = 1)
		{
			var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var ok = await adService.RejectAsync(id, reviewerId, reason);
			if (!ok) return NotFound();

			TempData["StatusMessage"] = "Ad rejected.";

			if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);

			return RedirectToAction(nameof(Pending), new { search, page });
		}
	}
}