// Services/BlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace WebTechnologiesLab1.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient? _blobServiceClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("BlobStorageConnectionString");
            _blobServiceClient = string.IsNullOrEmpty(connectionString)
                ? null
                : new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            if (file == null || file.Length == 0 || _blobServiceClient == null)
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

        // Services/BlobStorageService.cs

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (_blobServiceClient == null || !Uri.TryCreate(fileUrl, UriKind.Absolute, out Uri blobUri))
            {
                return;
            }

            string containerName = blobUri.Segments[1].Trim('/');
            string blobName = blobUri.Segments.Last();

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}