using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketZone.Controllers
{
	public class UserController : Controller
	{
		private readonly IUserService userService;

		public UserController(IUserService userService)
		{
			this.userService = userService;
		}

		[HttpGet]
		public async Task<IActionResult> Profile(string id, string? search,
		string? address,int? categoryId, decimal? minPrice, decimal? maxPrice,
		string? tags, string? sort,int page = 1)
		{
			var viewerId = User.Identity?.IsAuthenticated == true
				? User.FindFirstValue(ClaimTypes.NameIdentifier)
				: null;

			var model = await userService.GetProfileAsync(
				id, search, address, categoryId, minPrice, maxPrice, tags, sort,page,
				viewerId);

			return View(model);
		}

	}
}
