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

			// Load ad + seller
			var ad = await context.Ads
				.Include(a => a.User)
				.Include(a => a.Images)
				.AsNoTracking()
				.FirstOrDefaultAsync(a => a.Id == adId);

			if (ad == null)
				return null;

			// Must be either: (current is seller) or (current is buyer)
			// And otherUser must be the other side.
			bool currentIsSeller = ad.UserId == currentUserId;
			bool otherIsSeller = ad.UserId == otherUserId;

			// Valid pairs:
			// - currentIsSeller && !otherIsSeller
			// - !currentIsSeller && otherIsSeller
			if (currentIsSeller == otherIsSeller)
				return null;

			// Pull only messages for this exact conversation (two users + ad)
			var messages = await context.Messages
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
					SenderProfileImage = m.Sender.ProfilePictureUrl, // ✅ use real image
					Content = m.Content,
					ImageUrls = m.Images.Select(i => i.ImageUrl).ToList(),
					SentOn = m.SentOn
				})
				.AsNoTracking()
				.ToListAsync();

			// Determine other user's name
			string otherUserName;
			if (otherIsSeller)
			{
				otherUserName = ad.User.UserName!;
			}
			else
			{
				// other is buyer
				otherUserName = await context.Users
					.AsNoTracking()
					.Where(u => u.Id == otherUserId)
					.Select(u => u.UserName!)
					.FirstOrDefaultAsync() ?? "Unknown user";
			}

			var buyerId = currentIsSeller ? otherUserId : currentUserId;

			return new ChatViewModel
			{
				AdId = adId,
				ChatId = $"ad_{adId}_u_{buyerId}",
				OtherUserId = otherUserId,
				OtherUserName = otherUserName,
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

			// Validate that this receiver makes sense for this ad
			// One must be seller (ad owner), the other must be non-seller.
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

			// 1) Base query WITHOUT Include (important!)
			IQueryable<Message> baseQuery = context.Messages
				.AsNoTracking()
				.Where(m => m.SenderId == userId || m.ReceiverId == userId);

			// selling = chats for ads I own, buying = chats for ads I don't own
			if (mode == "selling")
				baseQuery = baseQuery.Where(m => m.Ad.UserId == userId);
			else
				baseQuery = baseQuery.Where(m => m.Ad.UserId != userId);

			// 2) Get only the latest message Id per Ad (still NO Include)
			// ✅ Group by (AdId + OtherUserId) so seller gets one thread per buyer per ad
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

			// 3) Now load the message rows we need WITH Include
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

					// if I'm sender -> other is receiver, else other is sender
					OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
					OtherUserName = m.SenderId == userId
						? m.Receiver.UserName!
						: m.Sender.UserName!,

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
