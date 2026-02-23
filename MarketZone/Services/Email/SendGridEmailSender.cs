using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MarketZone.Services.Implementations
{
	public class SendGridEmailSender : IEmailSender
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<SendGridEmailSender> _logger;

		public SendGridEmailSender(
			IConfiguration configuration,
			ILogger<SendGridEmailSender> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			var apiKey = _configuration["SendGrid:ApiKey"];
			var fromEmail = _configuration["SendGrid:FromEmail"];
			var fromName = _configuration["SendGrid:FromName"] ?? "MarketZone";

			if (string.IsNullOrWhiteSpace(apiKey))
				throw new InvalidOperationException("SendGrid API key is missing (SendGrid:ApiKey).");

			if (string.IsNullOrWhiteSpace(fromEmail))
				throw new InvalidOperationException("SendGrid FromEmail is missing (SendGrid:FromEmail).");

			if (string.IsNullOrWhiteSpace(email))
				throw new ArgumentException("Recipient email is empty.", nameof(email));

			// Create client per call is OK, but we can keep it here.
			// (If you want, you can inject ISendGridClient later.)
			var client = new SendGridClient(apiKey);

			var from = new EmailAddress(fromEmail, fromName);
			var to = new EmailAddress(email);

			var msg = MailHelper.CreateSingleEmail(
				from,
				to,
				subject,
				plainTextContent: StripHtml(htmlMessage),
				htmlContent: htmlMessage
			);

			// Retries for transient failures (429, 5xx)
			const int maxAttempts = 3;
			var delay = TimeSpan.FromMilliseconds(400);

			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					var response = await client.SendEmailAsync(msg);

					// 202 is success for SendGrid
					if ((int)response.StatusCode < 400)
					{
						_logger.LogInformation(
							"SendGrid email accepted. To={To} Subject={Subject} Status={Status}",
							email, subject, response.StatusCode);

						return;
					}

					var body = await response.Body.ReadAsStringAsync();

					_logger.LogWarning(
						"SendGrid email failed. Attempt {Attempt}/{MaxAttempts}. To={To} Status={Status} Body={Body}",
						attempt, maxAttempts, email, response.StatusCode, body);

					// Retry only for rate limit / transient server issues
					if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
						(int)response.StatusCode >= 500)
					{
						if (attempt < maxAttempts)
						{
							await Task.Delay(delay);
							delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
							continue;
						}
					}

					throw new InvalidOperationException(
						$"SendGrid failed: {response.StatusCode} - {body}");
				}
				catch (Exception ex) when (attempt < maxAttempts)
				{
					// Network / transient exception: retry
					_logger.LogWarning(
						ex,
						"SendGrid exception on attempt {Attempt}/{MaxAttempts}. To={To}",
						attempt, maxAttempts, email);

					await Task.Delay(delay);
					delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
				}
			}

			// If we get here, all attempts failed (should have thrown above)
			throw new InvalidOperationException("SendGrid failed after retries.");
		}

		private static string StripHtml(string html)
		{
			if (string.IsNullOrWhiteSpace(html)) return string.Empty;
			// keep it simple; you only need a fallback text body
			return html
				.Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
				.Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
				.Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
				.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
		}
	}
}