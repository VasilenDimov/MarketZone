using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketZone.Controllers
{
	[Authorize]
	public class FavoritesController : Controller
	{
		private readonly IFavoriteService favoriteService;

		public FavoritesController(IFavoriteService favoriteService)
		{
			this.favoriteService = favoriteService;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Toggle([FromBody] int adId)
		{
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			bool isFavorite = await favoriteService.ToggleAsync(adId, userId);

			return Json(new { isFavorite });
		}

		public async Task<IActionResult> MyFavorites()
		{
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var ads = await favoriteService.GetFavoritesAsync(userId);
			return View(ads);
		}
	}
}
