using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MarketZone.Services.Implementations
{
	public class SendGridEmailSender : IEmailSender
	{
		private readonly IConfiguration _configuration;

		public SendGridEmailSender(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			var apiKey = _configuration["SendGrid:ApiKey"];

			if (string.IsNullOrWhiteSpace(apiKey))
				throw new Exception("SendGrid API key is missing");

			var client = new SendGridClient(apiKey);

			var from = new EmailAddress(
				_configuration["SendGrid:FromEmail"],
				_configuration["SendGrid:FromName"]
			);

			var to = new EmailAddress(email);

			var msg = MailHelper.CreateSingleEmail(
				from,
				to,
				subject,
				plainTextContent: "Verification code",
				htmlContent: htmlMessage
			);

			var response = await client.SendEmailAsync(msg);

			if ((int)response.StatusCode >= 400)
			{
				var body = await response.Body.ReadAsStringAsync();
				throw new Exception($"SendGrid failed: {response.StatusCode} - {body}");
			}
		}

	}
}
