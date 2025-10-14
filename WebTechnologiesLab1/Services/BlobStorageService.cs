// Services/BlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace WebTechnologiesLab1.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("BlobStorageConnectionString"));
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            // Створюємо контейнер, якщо його не існує
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Створюємо унікальне ім'я для файлу, щоб уникнути конфліктів
            var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            // Повертаємо публічний URL до завантаженого файлу
            return blobClient.Uri.ToString();
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return;
            }

            Uri uri = new Uri(fileUrl);
            string containerName = uri.Segments[1].Trim('/');
            string blobName = uri.Segments[2];

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}