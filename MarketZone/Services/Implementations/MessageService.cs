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
				.Where(m =>
					m.AdId == adId &&
					(m.SenderId == userId || m.ReceiverId == userId))
				.Include(m => m.Sender)
				.Include(m => m.Images)
				.OrderBy(m => m.SentOn)
				.Select(m => new ChatMessageViewModel
				{
					SenderId = m.SenderId,
					SenderName = m.Sender.UserName!,
					SenderProfileImage = "/images/default-profile.png",
					Content = m.Content,
					ImageUrls = m.Images.Select(i => i.ImageUrl).ToList(),
					SentOn = m.SentOn
				})
				.ToListAsync();

			string otherUserName;

			if (ad.UserId == userId)
			{
				otherUserName = messages
					.Where(m => m.SenderId != userId)
					.Select(m => m.SenderName)
					.FirstOrDefault()
					?? "Unknown user";
			}
			else
			{
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

		public async Task SaveMessageAsync(int adId,string senderId,
		string? content,List<string> imageUrls)
		{
			var ad = await context.Ads.FindAsync(adId);

			if (ad == null)
				throw new InvalidOperationException("Ad not found.");

			string receiverId;

			if (senderId == ad.UserId)
			{
				receiverId = await context.Messages
					.Where(m => m.AdId == adId)
					.Select(m => m.SenderId)
					.FirstOrDefaultAsync(id => id != senderId)
					?? throw new InvalidOperationException("Receiver not found.");
			}
			else
			{
				receiverId = ad.UserId;
			}

			if (string.IsNullOrWhiteSpace(content) && !imageUrls.Any())
				throw new InvalidOperationException("Message must contain text or images.");

			var message = new Message
			{
				AdId = adId,
				SenderId = senderId,
				ReceiverId = receiverId,
				Content = content ?? string.Empty,
				SentOn = DateTime.UtcNow
			};

			context.Messages.Add(message);
			await context.SaveChangesAsync();

			if (imageUrls.Any())
			{
				foreach (var url in imageUrls)
				{
					context.MessageImages.Add(new MessageImage
					{
						MessageId = message.Id,
						ImageUrl = url
					});
				}

				await context.SaveChangesAsync();
			}
		}
		public async Task<InboxViewModel> GetInboxAsync(string userId, string mode)
		{
			var messagesQuery = context.Messages
				.Include(m => m.Ad)
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Where(m => m.SenderId == userId || m.ReceiverId == userId);

			if (mode == "selling")
			{
				messagesQuery = messagesQuery
					.Where(m => m.Ad.UserId == userId);
			}
			else
			{
				messagesQuery = messagesQuery
					.Where(m => m.Ad.UserId != userId);
			}

			var chats = messagesQuery
				.AsEnumerable()
				.GroupBy(m => m.AdId)
				.Select(g =>
				{
					var lastMessage = g
						.OrderByDescending(m => m.SentOn)
						.First();

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
