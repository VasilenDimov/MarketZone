#nullable disable

using System.ComponentModel.DataAnnotations;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketZone.Areas.Identity.Pages.Account.Manage
{
	public class IndexModel : PageModel
	{
		private const string DefaultAvatarUrl = "/images/default-avatar.png";

		private readonly UserManager<User> _userManager;
		private readonly SignInManager<User> _signInManager;
		private readonly IImageService _imageService;

		public IndexModel(
			UserManager<User> userManager,
			SignInManager<User> signInManager,
			IImageService imageService)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_imageService = imageService;
		}

		[TempData]
		public string StatusMessage { get; set; }

		// used for showing the current avatar
		public string ProfilePictureUrl { get; set; }

		[BindProperty]
		public InputModel Input { get; set; }

		// receives the uploaded file
		[BindProperty]
		public IFormFile ProfileImage { get; set; }

		public class InputModel
		{
			[Required]
			[Display(Name = "Username")]
			[StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between {2} and {1} characters.")]
			[RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can contain letters, numbers, dot, underscore and dash only.")]
			public string UserName { get; set; }

			[Phone]
			[Display(Name = "Phone number")]
			public string PhoneNumber { get; set; }
		}

		private async Task LoadAsync(User user)
		{
			var userName = await _userManager.GetUserNameAsync(user);
			var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

			ProfilePictureUrl = string.IsNullOrWhiteSpace(user.ProfilePictureUrl)
				? DefaultAvatarUrl
				: user.ProfilePictureUrl;

			Input = new InputModel
			{
				UserName = userName,
				PhoneNumber = phoneNumber
			};
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			await LoadAsync(user);
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
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

			// Username
			var currentUserName = await _userManager.GetUserNameAsync(user);
			var newUserName = Input.UserName?.Trim();

			if (!string.Equals(currentUserName, newUserName, StringComparison.Ordinal))
			{
				var existing = await _userManager.FindByNameAsync(newUserName);
				if (existing != null && existing.Id != user.Id)
				{
					ModelState.AddModelError("Input.UserName", "This username is already taken.");
					await LoadAsync(user);
					return Page();
				}

				var setUserNameResult = await _userManager.SetUserNameAsync(user, newUserName);
				if (!setUserNameResult.Succeeded)
				{
					foreach (var error in setUserNameResult.Errors)
						ModelState.AddModelError(string.Empty, error.Description);

					await LoadAsync(user);
					return Page();
				}
			}

			// Phone
			var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
			if (Input.PhoneNumber != phoneNumber)
			{
				var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
				if (!setPhoneResult.Succeeded)
				{
					StatusMessage = "Unexpected error when trying to set phone number.";
					return RedirectToPage();
				}
			}

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "Your profile has been updated.";
			return RedirectToPage();
		}

		// AJAX/FORM handler for avatar upload
		public async Task<IActionResult> OnPostProfilePictureAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			if (ProfileImage == null || ProfileImage.Length == 0)
				return BadRequest("No image selected.");

			string oldUrl = user.ProfilePictureUrl;

			string newUrl;
			try
			{
				newUrl = await _imageService.UploadProfileImageAsync(ProfileImage);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			user.ProfilePictureUrl = newUrl;
			var updateResult = await _userManager.UpdateAsync(user);
			if (!updateResult.Succeeded)
			{
				return BadRequest(string.Join(" ", updateResult.Errors.Select(e => e.Description)));
			}

			// delete old file (if it was uploaded and not default)
			if (!string.IsNullOrWhiteSpace(oldUrl) &&
				!string.Equals(oldUrl, DefaultAvatarUrl, StringComparison.OrdinalIgnoreCase) &&
				oldUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
			{
				await _imageService.DeleteImageAsync(oldUrl);
			}

			await _signInManager.RefreshSignInAsync(user);

			return new JsonResult(new { imageUrl = newUrl });
		}
	}
}