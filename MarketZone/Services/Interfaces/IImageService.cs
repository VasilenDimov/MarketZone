
namespace MarketZone.Services.Interfaces
{
	public interface IImageService
	{
		Task<string> UploadChatImageAsync(IFormFile image);
		Task<string> UploadAdImageAsync(IFormFile image);
		Task<string> UploadProfileImageAsync(IFormFile image);
		Task DeleteImageAsync(string imageUrl);
	}
}
