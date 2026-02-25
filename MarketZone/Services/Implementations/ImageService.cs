using MarketZone.Services.Interfaces;

namespace MarketZone.Services.Implementations
{
	public class ImageService : IImageService
	{
		private readonly IWebHostEnvironment env;

		private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

		private static readonly string[] AllowedExtensions =
			{ ".jpg", ".jpeg", ".png", ".webp" };

		private static readonly string[] AllowedMimeTypes =
		{
			"image/jpeg",
			"image/png",
			"image/webp"
		};
		public ImageService(IWebHostEnvironment env)
		{
			this.env = env;
		}

		public Task<string> UploadChatImageAsync(IFormFile image)
		{
			return UploadImageAsync(image, "chat");
		}

		public Task<string> UploadAdImageAsync(IFormFile image)
		{
			return UploadImageAsync(image, "ads");
		}
		public Task<string> UploadProfileImageAsync(IFormFile image)
		{
			return UploadImageAsync(image, "profile");
		}
		public Task DeleteImageAsync(string imageUrl)
		{
			if (string.IsNullOrWhiteSpace(imageUrl))
				return Task.CompletedTask;

			var physicalPath = Path.Combine(
				env.WebRootPath,
				imageUrl.TrimStart('/')
			);

			if (File.Exists(physicalPath))
			{
				File.Delete(physicalPath);
			}

			return Task.CompletedTask;
		}

		private async Task<string> UploadImageAsync(
			IFormFile image,
			string subFolder)
		{
			if (image == null || image.Length == 0)
				throw new ArgumentException("Image is empty.");

			if (image.Length > MaxFileSizeBytes)
				throw new ArgumentException("Image size exceeds limit.");

			var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

			if (!AllowedExtensions.Contains(extension))
				throw new ArgumentException("Invalid image extension.");

			if (!AllowedMimeTypes.Contains(image.ContentType))
				throw new ArgumentException("Invalid image MIME type.");

			var fileName = $"{Guid.NewGuid()}{extension}";
			var uploadPath = Path.Combine(env.WebRootPath, "uploads", subFolder);

			Directory.CreateDirectory(uploadPath);

			var fullPath = Path.Combine(uploadPath, fileName);

			using var stream = new FileStream(fullPath, FileMode.Create);
			await image.CopyToAsync(stream);

			return $"/uploads/{subFolder}/{fileName}";
		}
	}
}
