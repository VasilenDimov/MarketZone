#nullable disable

using MarketZone.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace MarketZone.Areas.Identity.Pages.Account
{
	public class LoginModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly IEmailSender _emailSender;
		private readonly ILogger<LoginModel> _logger;

		public LoginModel(
			SignInManager<User> signInManager,
			UserManager<User> userManager,
			IEmailSender emailSender,
			ILogger<LoginModel> logger)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_emailSender = emailSender;
			_logger = logger;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }

			[Required]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[Display(Name = "Remember me?")]
			public bool RememberMe { get; set; }
		}

		public async Task OnGetAsync(string returnUrl = null, string email = null)
		{
			if (!string.IsNullOrEmpty(ErrorMessage))
				ModelState.AddModelError(string.Empty, ErrorMessage);

			returnUrl ??= Url.Content("~/");

			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
			ReturnUrl = returnUrl;

			if (!string.IsNullOrWhiteSpace(email))
			{
				Input ??= new InputModel();
				Input.Email = email;
			}
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (!ModelState.IsValid)
				return Page();

			var user = await _userManager.FindByEmailAsync(Input.Email);

			if (user == null)
			{
				ModelState.AddModelError(string.Empty, "Invalid login attempt.");
				return Page();
			}

			// Prevent login for soft-deleted accounts
			if (user.IsDeleted)
			{
				ModelState.AddModelError(string.Empty, "This account has been deleted.");
				return Page();
			}

			if (!user.EmailConfirmed)
			{
				ModelState.AddModelError(string.Empty, "You must verify your email before logging in.");
				return Page();
			}

			var result = await _signInManager.PasswordSignInAsync(
				user.UserName,
				Input.Password,
				Input.RememberMe,
				lockoutOnFailure: false);

			if (result.Succeeded)
			{
				_logger.LogInformation("User logged in.");
				return LocalRedirect(returnUrl);
			}

			if (result.RequiresTwoFactor)
			{
				ModelState.AddModelError(string.Empty, "Two-factor authentication is disabled on this site.");
				return Page();
			}

			if (result.IsLockedOut)
				return RedirectToPage("./Lockout");

			ModelState.AddModelError(string.Empty, "Invalid login attempt.");
			return Page();
		}

		public async Task<IActionResult> OnPostResendConfirmationAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");

			var email = Input?.Email?.Trim();
			if (string.IsNullOrWhiteSpace(email))
			{
				TempData["StatusMessage"] = "Please enter your email first.";
				return RedirectToPage("./Login", new { returnUrl });
			}

			var user = await _userManager.FindByEmailAsync(email);

			if (user == null)
			{
				TempData["StatusMessage"] = "No account found with this email. No email was sent.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			if (user.IsDeleted)
			{
				TempData["StatusMessage"] = "This account has been deleted.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			var logins = await _userManager.GetLoginsAsync(user);
			if (logins.Any())
			{
				TempData["StatusMessage"] = "This account uses Google sign-in. Email confirmation resend is not available.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			if (user.EmailConfirmed)
			{
				TempData["StatusMessage"] = "This email is already confirmed. Please log in.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			var userId = await _userManager.GetUserIdAsync(user);
			var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
			code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

			var callbackUrl = Url.Page(
				"/Account/ConfirmEmail",
				pageHandler: null,
				values: new { area = "Identity", userId, code },
				protocol: Request.Scheme);

			await _emailSender.SendEmailAsync(
				email,
				"Confirm your email",
				$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

			TempData["StatusMessage"] = "Verification email sent. Please check your email.";

			TempData["StartCooldownKey"] = "login_resend";
			TempData["StartCooldownSeconds"] = 30;

			return RedirectToPage("./Login", new { returnUrl, email });
		}

		public async Task<IActionResult> OnPostForgotPasswordAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");

			var email = Input?.Email?.Trim();
			if (string.IsNullOrWhiteSpace(email))
			{
				TempData["StatusMessage"] = "Please enter your email first.";
				return RedirectToPage("./Login", new { returnUrl });
			}

			var user = await _userManager.FindByEmailAsync(email);

			if (user == null)
			{
				TempData["StatusMessage"] = "No account found with this email. No email was sent.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			if (user.IsDeleted)
			{
				TempData["StatusMessage"] = "This account has been deleted.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			var logins = await _userManager.GetLoginsAsync(user);
			if (logins.Any())
			{
				TempData["StatusMessage"] = "This account uses Google sign-in. Password reset is not available.";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			if (!user.EmailConfirmed)
			{
				TempData["StatusMessage"] = "Your email is not confirmed. Please confirm it first (or resend confirmation).";
				return RedirectToPage("./Login", new { returnUrl, email });
			}

			var code = await _userManager.GeneratePasswordResetTokenAsync(user);
			code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

			var callbackUrl = Url.Page(
				"/Account/ResetPassword",
				pageHandler: null,
				values: new { area = "Identity", code, email },
				protocol: Request.Scheme);

			await _emailSender.SendEmailAsync(
				email,
				"Reset Password",
				$"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

			TempData["StatusMessage"] = "Password reset email sent. Please check your email.";

			TempData["StartCooldownKey"] = "login_forgot";
			TempData["StartCooldownSeconds"] = 30;

			return RedirectToPage("./Login", new { returnUrl, email });
		}
	}
}