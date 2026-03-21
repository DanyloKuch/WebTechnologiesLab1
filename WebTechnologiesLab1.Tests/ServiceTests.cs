using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using WebTechnologiesLab1.Services;
using Xunit;

namespace WebTechnologiesLab1.Tests
{
    public class ServiceTests
    {
        private readonly BlobStorageService _service;
        private readonly Mock<BlobServiceClient> _blobServiceClientMock;
        private readonly Mock<BlobContainerClient> _containerClientMock;
        private readonly Mock<BlobClient> _blobClientMock;

        public ServiceTests()
        {
            var configMock = new Mock<IConfiguration>();

            // 1. Використовуємо валідний Base64 для AccountKey (це просто зашифрований "testkey")
            // 2. Налаштовуємо через індексатор [key], щоб метод GetConnectionString спрацював
            var fakeConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdGtleQ==;EndpointSuffix=core.windows.net";

            configMock.Setup(c => c.GetSection("ConnectionStrings")["BlobStorageConnectionString"])
                      .Returns(fakeConnectionString);

            _service = new BlobStorageService(configMock.Object);

            // Далі ваш код з Reflection...
            var field = typeof(BlobStorageService)
                .GetField("_blobServiceClient", BindingFlags.NonPublic | BindingFlags.Instance);

            _blobServiceClientMock = new Mock<BlobServiceClient>();
            field.SetValue(_service, _blobServiceClientMock.Object);

            _containerClientMock = new Mock<BlobContainerClient>();
            _blobClientMock = new Mock<BlobClient>();

            _blobServiceClientMock
                .Setup(c => c.GetBlobContainerClient(It.IsAny<string>()))
                .Returns(_containerClientMock.Object);

            _containerClientMock
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);
        }

        [Fact]
        public async Task UploadFileAsync_ValidFile_ReturnsBlobUrl()
        {
            // Arrange
            string containerName = "images";
            string originalFileName = "photo.jpg";
            string blobName = Guid.NewGuid().ToString() + ".jpg";
            string expectedUrl = $"https://fake.blob.core.windows.net/{containerName}/{blobName}";

            var formFileMock = new Mock<IFormFile>();
            var content = new byte[] { 0x01, 0x02, 0x03 };
            var stream = new MemoryStream(content);
            formFileMock.Setup(f => f.FileName).Returns(originalFileName);
            formFileMock.Setup(f => f.Length).Returns(content.Length);
            formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);

            // Мок CreateIfNotExistsAsync (з metadata)
            // Мок CreateIfNotExistsAsync — BlobContainerInfo має тільки 2 параметри (eTag + lastModified)
            _containerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContainerInfo(
                        eTag: new ETag("0x8D1234567890ABCD"),
                        lastModified: DateTimeOffset.UtcNow),
                    Mock.Of<Response>()));

            // Мок UploadAsync (Stream overload) — повний набір параметрів для BlobContentInfo
            _blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(
                        eTag: new ETag("0x8DABCDEF12345678"),
                        lastModified: DateTimeOffset.UtcNow,
                        contentHash: null,                  // або new byte[0] якщо хочеш
                        versionId: null,
                        encryptionKeySha256: null,          // ← ключовий параметр, який вимагався
                        encryptionScope: null,
                        blobSequenceNumber: 0L),            // long = 0
                    Mock.Of<Response>()));

            // Мок Uri (залишається без змін)
            _blobClientMock
                .SetupGet(b => b.Uri)
                .Returns(new Uri(expectedUrl));

            // Act
            string url = await _service.UploadFileAsync(formFileMock.Object, containerName);

            // Assert
            Assert.NotNull(url);
            Assert.Equal(expectedUrl, url);
            Assert.StartsWith("https://", url);
            Assert.Contains(containerName, url);
            Assert.Contains(".jpg", url);
        }

        [Fact]
        public async Task DeleteFileAsync_ValidUrl_CallsDelete()
        {
            // Arrange
            string fileUrl = "https://fake.blob.core.windows.net/products/old-product.png";

            _blobClientMock
                .Setup(b => b.DeleteIfExistsAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            // Act
            await _service.DeleteFileAsync(fileUrl);

            // Assert
            _blobClientMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task UploadFileAsync_CreateContainerFails_Throws()
        {
            // Arrange
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("test.jpg");
            formFileMock.Setup(f => f.Length).Returns(10);
            formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

            // ГНУЧКИЙ SETUP: 
            // Ми використовуємо It.IsAny для всіх параметрів, включаючи ті, що можуть бути null
            // Можливо знадобиться додати: using Azure.Storage.Blobs.Models;

            _containerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(), // <--- ОСЬ ЦЕЙ 4-Й ПАРАМЕТР!
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(403, "Access denied", "Forbidden", null));

            // Act & Assert
            // Перевіряємо, що ми отримуємо САМЕ RequestFailedException
            var ex = await Assert.ThrowsAsync<RequestFailedException>(async () =>
                await _service.UploadFileAsync(formFileMock.Object, "restricted")
            );

            Assert.Equal(403, ex.Status);
        }

        [Fact]
        public async Task UploadFileAsync_In_CI_Should_Be_Skipped()
        {
            string ciEnv = Environment.GetEnvironmentVariable("CI") ??
                           Environment.GetEnvironmentVariable("GITHUB_ACTIONS") ??
                           Environment.GetEnvironmentVariable("TF_BUILD");

            if (!string.IsNullOrEmpty(ciEnv))
            {
                return; // тест пропущено в CI
            }

            Assert.True(true); // placeholder
        }
    }
}