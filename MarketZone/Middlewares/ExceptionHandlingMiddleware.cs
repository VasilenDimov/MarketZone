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
				await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				logger.LogWarning(ex, ex.Message);
				await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unhandled exception");
				await HandleExceptionAsync(context, HttpStatusCode.InternalServerError,
					"An unexpected error occurred.");
			}
		}

		private static async Task HandleExceptionAsync(
			HttpContext context,
			HttpStatusCode statusCode,
			string message)
		{
			bool isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
					   || context.Request.Headers["Accept"].ToString().Contains("application/json");

			if (isAjax)
			{
				// API / AJAX call → return JSON 
				context.Response.StatusCode = (int)statusCode;
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(
					JsonSerializer.Serialize(new { error = message }));
			}
			else
			{
				// Browser navigation → redirect to error page 
				context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(message)}");
			}
		}
	}
}
