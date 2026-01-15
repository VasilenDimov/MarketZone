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
				.Include(a => a.Images)
				.FirstOrDefaultAsync(a => a.Id == adId);

			if (ad == null)
				return null;

			var messages = await context.Messages
				.Where(m => m.AdId == adId &&
					   (m.SenderId == userId || m.ReceiverId == userId))
				.Include(m => m.Sender)
				.OrderBy(m => m.SentOn)
				.Select(m => new ChatMessageViewModel
				{
					SenderId = m.SenderId,
					SenderName = m.Sender.UserName!,
					SenderProfileImage = "/images/default-profile.png",
					Content = m.Content,
					SentOn = m.SentOn
				})
				.ToListAsync();

			// Determine the OTHER user correctly
			string otherUserName;

			if (ad.UserId == userId)
			{
				// I am the seller → other user is the buyer
				otherUserName = messages
					.Where(m => m.SenderId != userId)
					.Select(m => m.SenderName)
					.FirstOrDefault() ?? "Unknown user";
			}
			else
			{
				// I am the buyer → other user is the seller
				otherUserName = ad.User.UserName!;
			}

			return new ChatViewModel
			{
				AdId = adId,
				ChatId = $"ad_{adId}",
				OtherUserName = otherUserName,
				Messages = messages,

				AdTitle = ad.Title,
				AdImageUrl = ad.Images
	                .OrderBy(i => i.Id)
	                .Select(i => i.ImageUrl)
					.FirstOrDefault() ?? "/images/no-image.png"
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
		public async Task<InboxViewModel> GetInboxAsync(string userId, string mode)
		{
			var messages = await context.Messages
				.Where(m => m.SenderId == userId || m.ReceiverId == userId)
				.Include(m => m.Ad)
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.OrderByDescending(m => m.SentOn)
				.ToListAsync();

			// Filter by role
			if (mode == "selling")
			{
				// User is seller → owns the ad
				messages = messages
					.Where(m => m.Ad.UserId == userId)
					.ToList();
			}
			else
			{
				// User is buyer → does NOT own the ad
				messages = messages
					.Where(m => m.Ad.UserId != userId)
					.ToList();
			}

			var chats = messages
				.GroupBy(m => m.AdId)
				.Select(g =>
				{
					var lastMessage = g.First();

					return new InboxChatItemViewModel
					{
						AdId = lastMessage.AdId,
						AdTitle = lastMessage.Ad.Title,
						OtherUserName = lastMessage.SenderId == userId
							? lastMessage.Receiver.UserName!
							: lastMessage.Sender.UserName!,
						LastMessage = lastMessage.Content,
						LastMessageTime = lastMessage.SentOn
					};
				})
				.OrderByDescending(c => c.LastMessageTime)
				.ToList();

			return new InboxViewModel
			{
				Mode = mode,
				Chats = chats
			};
		}
	}

}
