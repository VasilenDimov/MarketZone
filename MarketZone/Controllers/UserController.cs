using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
		public async Task<IActionResult> Profile(
			string id,
			string? search,
			string? sort)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				return NotFound();
			}

			var model = await userService.GetProfileAsync(id, search, sort);

			return View(model);
		}
	}
}
