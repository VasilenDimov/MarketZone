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
		var senderId = Context.UserIdentifier!;
		var sentOn = DateTime.UtcNow;

		await messageService.SaveMessageAsync(adId, senderId, message);

		await Clients.Group(chatId).SendAsync(
			"ReceiveMessage",
			senderId,
			message,
			sentOn.ToString("O") // ISO 8601 – JS SAFE
		);
	}

}
