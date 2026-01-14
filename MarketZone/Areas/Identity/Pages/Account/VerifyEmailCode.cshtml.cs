using System.Security.Cryptography;
using MarketZone.Data;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Areas.Identity.Pages.Account
{
	public class VerifyEmailCodeModel : PageModel
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<User> _userManager;
		private readonly IEmailSender _emailSender;

		public VerifyEmailCodeModel(
			ApplicationDbContext context,
			UserManager<User> userManager,
			IEmailSender emailSender)
		{
			_context = context;
			_userManager = userManager;
			_emailSender = emailSender;

			Input = new InputModel();
		}

		[BindProperty(SupportsGet = true)]
		public InputModel Input { get; set; }

		public string MaskedEmail { get; set; } = string.Empty;

		public string? ErrorMessage { get; set; }

		public IActionResult OnGet(string? email)
		{
			if (string.IsNullOrWhiteSpace(email))
				return RedirectToPage("Register");

			Input.Email = email;
			MaskedEmail = MaskEmail(email);

			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			MaskedEmail = MaskEmail(Input.Email);

			if (string.IsNullOrWhiteSpace(Input.Code) || Input.Code.Length != 6)
			{
				ErrorMessage = "Invalid or expired verification code.";
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(Input.Email);
			if (user == null)
			{
				ErrorMessage = "Invalid or expired verification code.";
				return Page();
			}

			var codeEntry = await _context.EmailVerificationCodes
				.Where(c => c.UserId == user.Id)
				.OrderByDescending(c => c.CreatedAt)
				.FirstOrDefaultAsync();

			if (codeEntry == null ||
				codeEntry.ExpiresAt < DateTime.UtcNow ||
				codeEntry.Code != Input.Code)
			{
				ErrorMessage = "Invalid or expired verification code.";
				return Page();
			}

			user.EmailConfirmed = true;
			await _userManager.UpdateAsync(user);

			_context.EmailVerificationCodes.RemoveRange(
				_context.EmailVerificationCodes.Where(c => c.UserId == user.Id));

			await _context.SaveChangesAsync();

			return RedirectToPage("Login");
		}

		public async Task<IActionResult> OnPostResendAsync([FromBody] ResendRequest request)

		{
			if (string.IsNullOrWhiteSpace(request.Email))
				return BadRequest();

			var user = await _userManager.FindByEmailAsync(request.Email);
			if (user == null)
				return BadRequest();

			var lastCode = await _context.EmailVerificationCodes
				.Where(c => c.UserId == user.Id)
				.OrderByDescending(c => c.CreatedAt)
				.FirstOrDefaultAsync();

			if (lastCode != null &&
				lastCode.CreatedAt.AddSeconds(30) > DateTime.UtcNow)
				return BadRequest();

			_context.EmailVerificationCodes.RemoveRange(
				_context.EmailVerificationCodes.Where(c => c.UserId == user.Id));

			var newCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

			_context.EmailVerificationCodes.Add(new EmailVerificationCode
			{
				UserId = user.Id,
				Code = newCode,
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddMinutes(10)
			});

			await _context.SaveChangesAsync();

			await _emailSender.SendEmailAsync(
	                user.Email!,
	                "MarketZone – verification code",
	                $@"
                    <p>Your new verification code is <b>{newCode}</b>.</p>
                    <p>This code expires in <b>10 minutes</b>.</p>"
            );
			return new JsonResult(true);
		}

		private static string MaskEmail(string email)
		{
			var parts = email.Split('@');
			return $"{parts[0][0]}***@{parts[1][0]}***";
		}
	}
	public class ResendRequest
	{
		public string Email { get; set; } = string.Empty;
	}
	public class InputModel
    {
	    public string Code { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}

}
