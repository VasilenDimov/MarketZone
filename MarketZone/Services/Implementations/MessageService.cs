using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Message;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class MessageService : IMessageService
	{
		private readonly ApplicationDbContext context;

		public MessageService(ApplicationDbContext context)
		{
			this.context = context;
		}

		public async Task<ChatViewModel?> GetChatAsync(int adId, string userId)
		{
			var ad = await context.Ads
				.Include(a => a.User)
				.FirstOrDefaultAsync(a => a.Id == adId);

			if (ad == null)
				return null;

			var messages = await context.Messages
				.Where(m => m.AdId == adId &&
					   (m.SenderId == userId || m.ReceiverId == userId))
				.OrderBy(m => m.SentOn)
				.Select(m => new ChatMessageViewModel
				{
					SenderName = m.Sender.UserName!,
					Content = m.Content,
					SentOn = m.SentOn
				})
				.ToListAsync();

			return new ChatViewModel
			{
				AdId = adId,
				ChatId = $"ad_{adId}",
				OtherUserName = ad.User.UserName!,
				Messages = messages
			};
		}


		public async Task SaveMessageAsync(int adId, string senderId, string content)
		{
			var ad = await context.Ads.FindAsync(adId);

			if (ad == null) return;

			var receiverId = ad.UserId == senderId
				? context.Messages
					.Where(m => m.AdId == adId && m.SenderId != senderId)
					.Select(m => m.SenderId)
					.FirstOrDefault()
				: ad.UserId;

			if (receiverId == null) return;

			context.Messages.Add(new Message
			{
				AdId = adId,
				SenderId = senderId,
				ReceiverId = receiverId,
				Content = content,
				SentOn = DateTime.UtcNow
			});

			await context.SaveChangesAsync();
		}
	}

}
