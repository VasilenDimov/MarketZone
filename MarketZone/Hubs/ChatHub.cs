using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[Authorize]
public class ChatHub : Hub
{
	private readonly IMessageService messageService;

	public ChatHub(IMessageService messageService)
	{
		this.messageService = messageService;
	}

	public async Task JoinChat(string chatId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
	}

	public async Task SendMessage(int adId, string chatId, string receiverId, string? content, List<string> imageUrls)
	{
		if (string.IsNullOrWhiteSpace(content) && !imageUrls.Any())
			return;

		var senderId = Context.UserIdentifier!;

		await messageService.SaveMessageAsync(
			adId,
			senderId,
			receiverId,
			content,
			imageUrls
		);

		await Clients.Group(chatId).SendAsync(
			"ReceiveMessage",
			senderId,
			content,
			imageUrls,
			DateTime.UtcNow.ToString("O")
		);
	}

}
