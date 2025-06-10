using APIDiscovery.Interfaces;

namespace APIDiscovery.Services;

public class ImageService : IImageService
{
    private readonly string _uploadsFolder;
    
    public ImageService()
    {
        _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(_uploadsFolder))
        {
            Directory.CreateDirectory(_uploadsFolder);
        }
    }
    
    public async Task<string> SaveImageAsync(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return null;
        }
        
        var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
        var filePath = Path.Combine(_uploadsFolder, uniqueFileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(fileStream);
        }
        
        return $"/uploads/{uniqueFileName}";
    }
    
    public void DeleteImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            return;
        }
        
        // Eliminar la parte inicial "/uploads/" para obtener solo el nombre del archivo
        var fileName = imagePath.Replace("/uploads/", "");
        var fullPath = Path.Combine(_uploadsFolder, fileName);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
    
    public async Task<string> UpdateImageAsync(string oldImagePath, IFormFile newImage)
    {
        // Si hay una imagen anterior, eliminarla
        DeleteImage(oldImagePath);
        
        // Guardar la nueva imagen
        return await SaveImageAsync(newImage);
    }
}