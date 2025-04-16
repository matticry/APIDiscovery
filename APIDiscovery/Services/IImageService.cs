namespace APIDiscovery.Services;

public interface IImageService
{
    Task<string> SaveImageAsync(IFormFile image);
    void DeleteImage(string imagePath);
    Task<string> UpdateImageAsync(string oldImagePath, IFormFile newImage);
}