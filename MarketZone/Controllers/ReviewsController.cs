using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketZone.Controllers
{
	[Authorize]
	public class ReviewController : Controller
	{
		private readonly IReviewService reviewService;
		private readonly IUserService userService;

		public ReviewController(
			IReviewService reviewService,
			IUserService userService)
		{
			this.reviewService = reviewService;
			this.userService = userService;
		}

		// EDIT (GET)
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			var model = await reviewService.GetForEditAsync(id);

			if (model == null)
				return NotFound();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			if (!await reviewService.CanUserEditAsync(id, userId))
				return Forbid();

			return View(model);
		}

		// EDIT (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(ReviewEditViewModel model)
		{
			if (!ModelState.IsValid)
				return View(model);

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			bool updated = await reviewService.UpdateAsync(model, userId);

			if (!updated)
				return Forbid();

			return RedirectToAction(
				"Profile",
				"User",
				new { id = model.ReviewedUserId });
		}

		// DELETE (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id, string reviewedUserId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			bool deleted = await reviewService.DeleteAsync(id, userId);

			if (!deleted)
				return Forbid();

			return RedirectToAction(
				"Profile",
				"User",
				new { id = reviewedUserId });
		}

		// CREATE (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ReviewCreateViewModel model)
		{
			var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (!ModelState.IsValid)
			{
				return View(
					"~/Views/User/Profile.cshtml",
					await userService.GetProfileAsync(
						userId: model.ReviewedUserId,
						search: null,
						address: null,
						categoryId: null,
						minPrice: null,
						maxPrice: null,
						tags: null,
						sort: null,
						page: 1,
						viewerId: viewerId
					));
			}

			var reviewerId = viewerId!;

			await reviewService.AddReviewAsync(reviewerId, model);

			return RedirectToAction(
				"Profile",
				"User",
				new { id = model.ReviewedUserId });
		}
	}
}
