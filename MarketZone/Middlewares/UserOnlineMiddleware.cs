using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace MarketZone.Middlewares
{
	public class UserOnlineMiddleware
	{
		private readonly RequestDelegate _next;

		public UserOnlineMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(
			HttpContext context,
			UserManager<User> userManager)
		{
			if (context.User?.Identity?.IsAuthenticated == true)
			{
				var user = await userManager.GetUserAsync(context.User);

				if (user != null)
				{
					var now = DateTime.UtcNow;

					if (user.LastOnlineOn == null ||
						now - user.LastOnlineOn > TimeSpan.FromMinutes(2))
					{
						user.LastOnlineOn = now;
						await userManager.UpdateAsync(user);
					}
				}
			}

			await _next(context);
		}
	}

}
