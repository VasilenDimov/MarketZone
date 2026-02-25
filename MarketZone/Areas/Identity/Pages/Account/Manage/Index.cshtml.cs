#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Http;
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

		public string ProfilePictureUrl { get; set; }

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			public string ProfileImageBase64 { get; set; }

			[Required]
			[Display(Name = "Username")]
			[StringLength(256, MinimumLength = 3, ErrorMessage = "Username must be between {2} and {1} characters.")]
			[RegularExpression(@"^[a-zA-Z0-9._@-]+$", ErrorMessage = "Username can contain letters, numbers, @, dot, underscore and dash only.")]
			public string UserName { get; set; }

		}

		private async Task LoadAsync(User user)
		{
			var userName = await _userManager.GetUserNameAsync(user);

			ProfilePictureUrl = string.IsNullOrWhiteSpace(user.ProfilePictureUrl)
				? DefaultAvatarUrl
				: user.ProfilePictureUrl;

			Input = new InputModel
			{
				UserName = userName,
				ProfileImageBase64 = null
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
					{
						ModelState.AddModelError(string.Empty, error.Description);
					}

					await LoadAsync(user);
					return Page();
				}
			}

			if (!string.IsNullOrWhiteSpace(Input.ProfileImageBase64))
			{
				var oldUrl = user.ProfilePictureUrl;

				if (!TryParseDataUri(Input.ProfileImageBase64, out var contentType, out var bytes, out var fileExtension))
				{
					ModelState.AddModelError(string.Empty, "Invalid image data.");
					await LoadAsync(user);
					return Page();
				}

				IFormFile formFile;
				try
				{
					var ms = new MemoryStream(bytes);
					formFile = new FormFile(ms, 0, bytes.Length, "ProfileImage", $"avatar.{fileExtension}")
					{
						Headers = new HeaderDictionary(),
						ContentType = contentType
					};
				}
				catch
				{
					ModelState.AddModelError(string.Empty, "Could not process the selected image.");
					await LoadAsync(user);
					return Page();
				}

				string newUrl;
				try
				{
					newUrl = await _imageService.UploadProfileImageAsync(formFile);
				}
				catch (Exception ex)
				{
					ModelState.AddModelError(string.Empty, ex.Message);
					await LoadAsync(user);
					return Page();
				}

				user.ProfilePictureUrl = newUrl;
				var updateResult = await _userManager.UpdateAsync(user);
				if (!updateResult.Succeeded)
				{
					ModelState.AddModelError(string.Empty, string.Join(" ", updateResult.Errors.Select(e => e.Description)));
					await LoadAsync(user);
					return Page();
				}

				if (!string.IsNullOrWhiteSpace(oldUrl) &&
					!string.Equals(oldUrl, DefaultAvatarUrl, StringComparison.OrdinalIgnoreCase) &&
					oldUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
				{
					await _imageService.DeleteImageAsync(oldUrl);
				}
			}

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "Your profile has been updated.";
			return RedirectToPage();
		}

		private static bool TryParseDataUri(string dataUri, out string contentType, out byte[] bytes, out string fileExtension)
		{
			contentType = null;
			bytes = null;
			fileExtension = "png";

			var commaIndex = dataUri.IndexOf(',');
			if (commaIndex <= 0)
				return false;

			var header = dataUri[..commaIndex];
			var base64 = dataUri[(commaIndex + 1)..];

			if (!header.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
				!header.Contains(";base64", StringComparison.OrdinalIgnoreCase))
				return false;

			contentType = header.Substring("data:".Length, header.IndexOf(';') - "data:".Length).Trim();

			fileExtension = contentType.ToLowerInvariant() switch
			{
				"image/jpeg" => "jpg",
				"image/jpg" => "jpg",
				"image/png" => "png",
				"image/webp" => "webp",
				_ => null
			};

			if (fileExtension == null)
				return false;

			try
			{
				bytes = Convert.FromBase64String(base64);
				return bytes.Length > 0;
			}
			catch
			{
				return false;
			}
		}
	}
}