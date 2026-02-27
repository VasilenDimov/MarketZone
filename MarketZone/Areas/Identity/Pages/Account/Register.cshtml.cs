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
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly IUserStore<User> _userStore;
		private readonly IUserEmailStore<User> _emailStore;
		private readonly ILogger<RegisterModel> _logger;
		private readonly IEmailSender _emailSender;

		public RegisterModel(
			UserManager<User> userManager,
			IUserStore<User> userStore,
			SignInManager<User> signInManager,
			ILogger<RegisterModel> logger,
			IEmailSender emailSender)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		[TempData]
		public string InfoMessage { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }

			[Required]
			[StringLength(100, MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }
		}

		public async Task OnGetAsync(string returnUrl = null, string email = null)
		{
			ReturnUrl = returnUrl ?? Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (!string.IsNullOrWhiteSpace(email))
			{
				Input ??= new InputModel();
				Input.Email = email;
			}
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			ReturnUrl = returnUrl ?? Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (!ModelState.IsValid)
				return Page();

			var existingUser = await _userManager.FindByEmailAsync(Input.Email);
			if (existingUser != null)
			{
				ModelState.AddModelError("Input.Email", "An account with this email already exists.");
				return Page();
			}

			var user = CreateUser();
			user.CreatedOn = DateTime.UtcNow;

			await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
			await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

			var result = await _userManager.CreateAsync(user, Input.Password);

			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
				{
					if (error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName")
						ModelState.AddModelError("Input.Email", "An account with this email already exists.");
					else
						ModelState.AddModelError(string.Empty, error.Description);
				}

				return Page();
			}

			_logger.LogInformation("User created a new account with password.");

			await SendConfirmationEmailAsync(user, Input.Email);
			InfoMessage = "Confirmation email sent. Please check your email.";

			return RedirectToPage("./Register", new { returnUrl = ReturnUrl, email = Input.Email });
		}

		public async Task<IActionResult> OnPostResendConfirmationAsync(string returnUrl = null)
		{
			ReturnUrl = returnUrl ?? Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			ModelState.Remove("Input.Password");
			ModelState.Remove("Input.ConfirmPassword");

			if (!ModelState.IsValid)
				return Page();

			var email = Input?.Email?.Trim();
			if (string.IsNullOrWhiteSpace(email))
			{
				ModelState.AddModelError("Input.Email", "Please enter your email first.");
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(email);

			if (user == null)
			{
				InfoMessage = "No account found with this email. No email was sent.";
				return RedirectToPage("./Register", new { returnUrl = ReturnUrl, email });
			}

			var logins = await _userManager.GetLoginsAsync(user);
			if (logins.Any())
			{
				InfoMessage = "This account uses Google sign-in. Email confirmation resend is not available.";
				return RedirectToPage("./Register", new { returnUrl = ReturnUrl, email });
			}

			if (user.EmailConfirmed)
			{
				InfoMessage = "This email is already confirmed. Please log in.";
				return RedirectToPage("./Register", new { returnUrl = ReturnUrl, email });
			}

			await SendConfirmationEmailAsync(user, email);

			InfoMessage = "Confirmation email resent. Please check your email.";

			TempData["StartCooldownKey"] = "register_resend";
			TempData["StartCooldownSeconds"] = 30;

			return RedirectToPage("./Register", new { returnUrl = ReturnUrl, email });
		}

		private async Task SendConfirmationEmailAsync(User user, string email)
		{
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
		}

		private User CreateUser()
		{
			try
			{
				return Activator.CreateInstance<User>();
			}
			catch
			{
				throw new InvalidOperationException(
					$"Can't create an instance of '{nameof(User)}'. " +
					$"Ensure that '{nameof(User)}' is not abstract and has a parameterless constructor."
				);
			}
		}

		private IUserEmailStore<User> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
				throw new NotSupportedException("The default UI requires a user store with email support.");

			return (IUserEmailStore<User>)_userStore;
		}
	}
}