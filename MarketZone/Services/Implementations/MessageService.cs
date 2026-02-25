using MarketZone.Common;
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

		public async Task<ChatViewModel?> GetChatAsync(int adId, string currentUserId, string otherUserId)
		{
			if (string.IsNullOrWhiteSpace(otherUserId))
				return null;

			var ad = await context.Ads
				.AsNoTracking()
				.Include(a => a.User)
				.Include(a => a.Images)
				.FirstOrDefaultAsync(a => a.Id == adId);

			if (ad == null)
				return null;

			bool currentIsSeller = ad.UserId == currentUserId;
			bool otherIsSeller = ad.UserId == otherUserId;

			if (currentIsSeller == otherIsSeller)
				return null;

			var otherUser = await context.Users
				.AsNoTracking()
				.Where(u => u.Id == otherUserId)
				.Select(u => new { u.Id, u.UserName, u.ProfilePictureUrl })
				.FirstOrDefaultAsync();

			if (otherUser == null)
				return null;

			var messages = await context.Messages
				.AsNoTracking()
				.Where(m => m.AdId == adId &&
					((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
					 (m.SenderId == otherUserId && m.ReceiverId == currentUserId)))
				.Include(m => m.Sender)
				.Include(m => m.Images)
				.OrderBy(m => m.SentOn)
				.Select(m => new ChatMessageViewModel
				{
					SenderId = m.SenderId,
					SenderName = m.Sender.UserName!,
					SenderProfileImage = string.IsNullOrWhiteSpace(m.Sender.ProfilePictureUrl)
						? AppConstants.DefaultAvatarUrl
						: m.Sender.ProfilePictureUrl,
					Content = m.Content,
					ImageUrls = m.Images.Select(i => i.ImageUrl).ToList(),
					SentOn = m.SentOn
				})
				.ToListAsync();

			var buyerId = currentIsSeller ? otherUserId : currentUserId;

			return new ChatViewModel
			{
				AdId = adId,
				ChatId = $"ad_{adId}_u_{buyerId}",
				OtherUserId = otherUserId,
				OtherUserName = otherUser.UserName ?? "Unknown user",
				OtherUserProfilePictureUrl = string.IsNullOrWhiteSpace(otherUser.ProfilePictureUrl)
					? AppConstants.DefaultAvatarUrl
					: otherUser.ProfilePictureUrl,
				CurrentUserId = currentUserId,
				Messages = messages,
				AdTitle = ad.Title,
				AdImageUrl = ad.Images
					.OrderBy(i => i.Id)
					.Select(i => i.ImageUrl)
					.FirstOrDefault() ?? "/images/no-image.png"
			};
		}

		public async Task SaveMessageAsync(int adId, string senderId, string receiverId, string? content, List<string> imageUrls)
		{
			var ad = await context.Ads.AsNoTracking().FirstOrDefaultAsync(a => a.Id == adId);
			if (ad == null)
				throw new InvalidOperationException("Ad not found.");

			if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
				throw new InvalidOperationException("Invalid sender/receiver.");

			bool senderIsSeller = ad.UserId == senderId;
			bool receiverIsSeller = ad.UserId == receiverId;
			if (senderIsSeller == receiverIsSeller)
				throw new InvalidOperationException("Invalid conversation participants.");

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
			mode = (mode ?? "buying").ToLowerInvariant();

			IQueryable<Message> baseQuery = context.Messages
				.AsNoTracking()
				.Where(m => m.SenderId == userId || m.ReceiverId == userId);

			if (mode == "selling")
				baseQuery = baseQuery.Where(m => m.Ad.UserId == userId);
			else
				baseQuery = baseQuery.Where(m => m.Ad.UserId != userId);

			var latestMessageIds = await baseQuery
				.Select(m => new
				{
					m.Id,
					m.AdId,
					m.SentOn,
					OtherUserId = (m.SenderId == userId) ? m.ReceiverId : m.SenderId
				})
				.GroupBy(x => new { x.AdId, x.OtherUserId })
				.Select(g => g
					.OrderByDescending(x => x.SentOn)
					.Select(x => x.Id)
					.First())
				.ToListAsync();

			var chats = await context.Messages
				.AsNoTracking()
				.Where(m => latestMessageIds.Contains(m.Id))
				.Include(m => m.Ad)
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Select(m => new InboxChatItemViewModel
				{
					AdId = m.AdId,
					AdTitle = m.Ad.Title,
					OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
					OtherUserName = m.SenderId == userId
						? m.Receiver.UserName!
						: m.Sender.UserName!,
					OtherUserProfilePictureUrl = m.SenderId == userId
	               ? (string.IsNullOrWhiteSpace(m.Receiver.ProfilePictureUrl)
				   ? AppConstants.DefaultAvatarUrl : m.Receiver.ProfilePictureUrl)
	               : (string.IsNullOrWhiteSpace(m.Sender.ProfilePictureUrl)
				   ? AppConstants.DefaultAvatarUrl : m.Sender.ProfilePictureUrl),
					LastMessage = m.Content,
					LastMessageTime = m.SentOn
				})
				.OrderByDescending(c => c.LastMessageTime)
				.ToListAsync();

			return new InboxViewModel
			{
				Mode = mode,
				Chats = chats
			};
		}
	}
}