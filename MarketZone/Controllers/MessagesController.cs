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
		[HttpPost]
		public async Task<IActionResult> UploadChatImage(IFormFile image)
		{
			if (image == null || image.Length == 0)
				return BadRequest();

			var ext = Path.GetExtension(image.FileName).ToLower();
			var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

			if (!allowed.Contains(ext))
				return BadRequest();

			var fileName = $"{Guid.NewGuid()}{ext}";
			var path = Path.Combine("wwwroot/uploads/chat", fileName);

			Directory.CreateDirectory(Path.GetDirectoryName(path)!);

			using var stream = new FileStream(path, FileMode.Create);
			await image.CopyToAsync(stream);

			return Json(new { imageUrl = $"/uploads/chat/{fileName}" });
		}

	}

}
