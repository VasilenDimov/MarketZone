using MarketZone.Services.Interfaces;
using MarketZone.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace MarketZone.Controllers
{
	public class HomeController : Controller
	{
		private readonly IAdService adService;

		public HomeController(IAdService adService)
		{
			this.adService = adService;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string? search,string? address,
	    int? categoryId,decimal? minPrice,decimal? maxPrice,string? tags,
	    string? sort,int page = 1)
		{
			string? userId = User.Identity?.IsAuthenticated == true
				? User.FindFirstValue(ClaimTypes.NameIdentifier)
				: null;

			var model = await adService.SearchAsync(search,address,categoryId,
			minPrice,maxPrice,tags,sort,page,userId
			);

			return View(model);
		}


		[AllowAnonymous]
		public IActionResult Error(string? message)
		{
			var model = new ErrorViewModel
			{
				RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
				Message = message
			};
			return View(model);
		}
	}

}
