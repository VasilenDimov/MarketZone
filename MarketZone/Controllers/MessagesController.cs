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
		private readonly IImageService imageService;

		public MessagesController(IMessageService messageService,IImageService imageService)
		{
			this.messageService = messageService;
			this.imageService = imageService;
		}

		public async Task<IActionResult> Chat(int adId, string otherUserId)
		{
			var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var chat = await messageService.GetChatAsync(adId, currentUserId, otherUserId);
			if (chat == null)
				return NotFound();

			return View(chat);
		}

		public async Task<IActionResult> Inbox(string mode = "buying")
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var model = await messageService.GetInboxAsync(userId, mode);

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> UploadChatImage(IFormFile image)
		{
			var imageUrl = await imageService.UploadChatImageAsync(image);
			return Json(new { imageUrl });
		}
	}
}
