#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace MarketZone.Areas.Identity.Pages.Account.Manage
{
	public class EmailModel : PageModel
	{
		private readonly UserManager<User> _userManager;
		private readonly SignInManager<User> _signInManager;
		private readonly IEmailSender _emailSender;

		public EmailModel(
			UserManager<User> userManager,
			SignInManager<User> signInManager,
			IEmailSender emailSender)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_emailSender = emailSender;
		}
		public string Email { get; set; }

		public bool IsEmailConfirmed { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		[TempData]
		public string PendingNewEmail { get; set; }

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			[Display(Name = "New email")]
			public string NewEmail { get; set; }
		}

		private async Task LoadAsync(User user)
		{
			Email = await _userManager.GetEmailAsync(user);
			IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

			Input ??= new InputModel();
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			await LoadAsync(user);

			if (!string.IsNullOrWhiteSpace(PendingNewEmail))
			{
				Input.NewEmail = PendingNewEmail;
			}

			return Page();
		}

		public async Task<IActionResult> OnPostChangeEmailAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			if (!ModelState.IsValid)
			{
				await LoadAsync(user);
				return Page();
			}

			// Require password for external-only (Google-only) accounts
			if (!await _userManager.HasPasswordAsync(user))
			{
				StatusMessage = "You must set a password before changing your email.";
				return RedirectToPage("./SetPassword");
			}

			var email = await _userManager.GetEmailAsync(user);

			if (Input.NewEmail != email)
			{
				// Prevent using an already existing email
				var existingUser = await _userManager.FindByEmailAsync(Input.NewEmail);
				if (existingUser != null)
				{
					ModelState.AddModelError(string.Empty, "An account with this email already exists.");
					await LoadAsync(user);
					Input.NewEmail = Input.NewEmail; // keep typed value
					return Page();
				}

				var userId = await _userManager.GetUserIdAsync(user);
				var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
				code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

				var callbackUrl = Url.Page(
					"/Account/ConfirmEmailChange",
					pageHandler: null,
					values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
					protocol: Request.Scheme);

				await _emailSender.SendEmailAsync(
					Input.NewEmail,
					"Confirm your email",
					$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

				// Trigger cooldown
				TempData["AuthCooldownStartKey"] = "change_email";
				TempData["AuthCooldownStartSeconds"] = 30;

				StatusMessage = "Confirmation link to change email sent. Please check your email.";
				return RedirectToPage();
			}

			StatusMessage = "Your email is unchanged.";
			return RedirectToPage();
		}

		public async Task<IActionResult> OnPostSendVerificationEmailAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			if (!ModelState.IsValid)
			{
				await LoadAsync(user);
				return Page();
			}

			var userId = await _userManager.GetUserIdAsync(user);
			var email = await _userManager.GetEmailAsync(user);
			var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
			code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
			var callbackUrl = Url.Page(
				"/Account/ConfirmEmail",
				pageHandler: null,
				values: new { area = "Identity", userId = userId, code = code },
				protocol: Request.Scheme);
			await _emailSender.SendEmailAsync(
				email,
				"Confirm your email",
				$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

			StatusMessage = "Verification email sent. Please check your email.";
			return RedirectToPage();
		}
	}
}