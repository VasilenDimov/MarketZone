using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;

namespace MarketZone.Controllers
{
	//[Authorize]
	public class AdController : Controller
	{
		private readonly IAdService adService;

		public AdController(IAdService adService)
		{
			this.adService = adService;
		}

		// GET
		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}

		// POST
		[HttpPost]
		public async Task<IActionResult> Create(AdCreateModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			int adId = await adService.CreateAsync(model, userId);

			return RedirectToAction("Details", new { id = adId });
		}
	}
}
