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

	public async Task SendMessage(int adId, string chatId, string message)
	{
		var senderId = Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var senderName = Context.User.Identity!.Name!;

		await messageService.SaveMessageAsync(adId, senderId, message);

		await Clients.Group(chatId).SendAsync(
			"ReceiveMessage",
			senderName,
			message,
			DateTime.UtcNow.ToString("HH:mm")
		);
	}
}
