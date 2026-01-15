using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketZone.Controllers
{
	[Authorize]
	public class MessagesController : Controller
	{
		private readonly IMessageService messageService;

		public MessagesController(IMessageService messageService)
		{
			this.messageService = messageService;
		}

		public async Task<IActionResult> Chat(int adId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var model = await messageService.GetChatAsync(adId, userId);
			if (model == null)
				return NotFound();

			model.CurrentUserId = userId;

			return View(model);
		}

		public async Task<IActionResult> Inbox(string mode = "buying")
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var model = await messageService.GetInboxAsync(userId, mode);

			return View(model);
		}
	}

}
