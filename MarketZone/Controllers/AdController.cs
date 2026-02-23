using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;

namespace MarketZone.Controllers
{
	[Authorize]
	public class AdController : Controller
	{
		private readonly IAdService adService;
		private readonly ICategoryService categoryService;

		public AdController(
			IAdService adService,
			ICategoryService categoryService)
		{
			this.adService = adService;
			this.categoryService = categoryService;
		}

		// GET: /Ad/Create
		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var model = new AdCreateModel
			{
				Categories = await categoryService.GetAllAsync()
			};

			return View(model);
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			string? userId = User.Identity!.IsAuthenticated
				? User.FindFirstValue(ClaimTypes.NameIdentifier)
				: null;

			var model = await adService.GetDetailsAsync(id, userId);

			if (model == null)
				return NotFound();

			return View(model);
		}


		[HttpGet]
		public async Task<IActionResult> MyAds()
		{
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var ads = await adService.GetMyAdsAsync(userId);

			return View(ads);
		}
		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var model = await adService.GetEditModelAsync(id, userId);

			if (model == null)
			{
				return NotFound();
			}

			return View(model);
		}
		[HttpGet]
		public async Task<IActionResult> GetCategoryPath(int categoryId)
		{
			var path = await categoryService.GetCategoryPathAsync(categoryId);
			return Json(path);
		}
		// GET: /Ad/GetChildren
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetChildren(int? parentId)
		{
			var categories = await categoryService.GetChildrenAsync(parentId);
			return Json(categories);
		}

		// POST: /Ad/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(AdCreateModel model)
		{
			if (!ModelState.IsValid)
			{
				model.Categories = await categoryService.GetAllAsync();
				return View(model);
			}

			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			int adId = await adService.CreateAsync(model, userId);

			return RedirectToAction("Details", new { id = adId });
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(AdCreateModel model)
		{
			if (!ModelState.IsValid)
			{
				model.Categories = await categoryService.GetAllAsync();
				return View(model);
			}

			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			bool updated = await adService.UpdateAsync(model, userId);

			if (!updated)
				return Forbid();

			return RedirectToAction("Details", new { id = model.Id });
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var success = await adService.DeleteAsync(id, userId!);

			if (!success)
				return BadRequest();

			return RedirectToAction(nameof(MyAds));
		}

	}
}
