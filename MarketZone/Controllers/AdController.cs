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
			var model = await adService.GetDetailsAsync(id);

			if (model == null)
			{
				return NotFound();
			}

			return View(model);
		}

		// GET: /Ad/GetChildren
		[HttpGet]
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
	}
}
