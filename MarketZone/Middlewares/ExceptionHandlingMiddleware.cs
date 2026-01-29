using System.Net;
using System.Text.Json;

namespace MarketZone.Middlewares
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ILogger<ExceptionHandlingMiddleware> logger;

		public ExceptionHandlingMiddleware(
			RequestDelegate next,
			ILogger<ExceptionHandlingMiddleware> logger)
		{
			this.next = next;
			this.logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await next(context);
			}
			catch (ArgumentException ex)
			{
				logger.LogWarning(ex, ex.Message);
				await WriteResponseAsync(context, HttpStatusCode.BadRequest, ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				logger.LogWarning(ex, ex.Message);
				await WriteResponseAsync(context, HttpStatusCode.BadRequest, ex.Message);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unhandled exception");
				await WriteResponseAsync(
					context,
					HttpStatusCode.InternalServerError,
					"An unexpected error occurred.");
			}
		}

		private static async Task WriteResponseAsync(
			HttpContext context,
			HttpStatusCode statusCode,
			string message)
		{
			context.Response.StatusCode = (int)statusCode;
			context.Response.ContentType = "application/json";

			var payload = JsonSerializer.Serialize(new
			{
				error = message
			});

			await context.Response.WriteAsync(payload);
		}
	}
}
