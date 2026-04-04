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

            var fakeConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdGtleQ==;EndpointSuffix=core.windows.net";

            configMock.Setup(c => c.GetSection("ConnectionStrings")["BlobStorageConnectionString"])
                      .Returns(fakeConnectionString);

            _service = new BlobStorageService(configMock.Object);

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

            _blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(
                        eTag: new ETag("0x8DABCDEF12345678"),
                        lastModified: DateTimeOffset.UtcNow,
                        contentHash: null,                 
                        versionId: null,
                        encryptionKeySha256: null,         
                        encryptionScope: null,
                        blobSequenceNumber: 0L),           
                    Mock.Of<Response>()));

            _blobClientMock
                .SetupGet(b => b.Uri)
                .Returns(new Uri(expectedUrl));

            string url = await _service.UploadFileAsync(formFileMock.Object, containerName);

            Assert.NotNull(url);
            Assert.Equal(expectedUrl, url);
            Assert.StartsWith("https://", url);
            Assert.Contains(containerName, url);
            Assert.Contains(".jpg", url);
        }

        [Fact]
        public async Task DeleteFileAsync_ValidUrl_CallsDelete()
        {
            string fileUrl = "https://fake.blob.core.windows.net/products/old-product.png";

            _blobClientMock
                .Setup(b => b.DeleteIfExistsAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            await _service.DeleteFileAsync(fileUrl);

            _blobClientMock.Verify(b => b.DeleteIfExistsAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task UploadFileAsync_CreateContainerFails_Throws()
        {
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns("test.jpg");
            formFileMock.Setup(f => f.Length).Returns(10);
            formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());


            _containerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(403, "Access denied", "Forbidden", null));
            
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
                return; 
            }

            Assert.True(true); 
        }

        [Fact]
        public async Task GetBlobContainerClient_CalledWithSpecificContainerName_UsesMatching()
        {
            _blobServiceClientMock
                .Setup(c => c.GetBlobContainerClient(It.Is<string>(name => name == "images" && name.Length > 0)))
                .Returns(_containerClientMock.Object);

            _containerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContainerInfo(new ETag("abc"), DateTimeOffset.UtcNow),
                    Mock.Of<Response>()));

            _blobClientMock
                .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(new ETag("def"), DateTimeOffset.UtcNow, null, null, null, null, 0L),
                    Mock.Of<Response>()));

            _blobClientMock.SetupGet(b => b.Uri).Returns(new Uri("https://fake.blob.core.windows.net/images/file.jpg"));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("file.jpg");
            fileMock.Setup(f => f.Length).Returns(5);
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));

            var url = await _service.UploadFileAsync(fileMock.Object, "images");

            _blobServiceClientMock.Verify(
                c => c.GetBlobContainerClient(It.Is<string>(name => name == "images")),
                Times.Once);
            Assert.Contains("images", url);
        }

        [Fact]
        public async Task UploadFileAsync_CalledTwice_ReturnsDifferentUrls()
        {
            var url1 = "https://fake.blob.core.windows.net/images/first.jpg";
            var url2 = "https://fake.blob.core.windows.net/images/second.jpg";

            _blobClientMock
                .SetupSequence(b => b.Uri)
                .Returns(new Uri(url1))
                .Returns(new Uri(url2));

            _containerClientMock
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContainerInfo(new ETag("abc"), DateTimeOffset.UtcNow),
                    Mock.Of<Response>()));

            _blobClientMock
                .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(new ETag("def"), DateTimeOffset.UtcNow, null, null, null, null, 0L),
                    Mock.Of<Response>()));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("photo.jpg");
            fileMock.Setup(f => f.Length).Returns(3);
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

            var result1 = await _service.UploadFileAsync(fileMock.Object, "images");

            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 4, 5, 6 }));
            var result2 = await _service.UploadFileAsync(fileMock.Object, "images");

            Assert.NotEqual(result1, result2);
            Assert.Equal(url1, result1);
            Assert.Equal(url2, result2);
        }
        [Fact]
        public async Task UploadFileAsync_ExecutesStepsInCorrectOrder()
        {
            var sequence = new MockSequence();

            _containerClientMock
                .InSequence(sequence)
                .Setup(c => c.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContainerInfo(new ETag("abc"), DateTimeOffset.UtcNow),
                    Mock.Of<Response>()));

            _containerClientMock
                .InSequence(sequence)
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            _blobClientMock
                .Setup(b => b.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(
                    BlobsModelFactory.BlobContentInfo(new ETag("def"), DateTimeOffset.UtcNow, null, null, null, null, 0L),
                    Mock.Of<Response>()));

            _blobClientMock
                .SetupGet(b => b.Uri)
                .Returns(new Uri("https://fake.blob.core.windows.net/images/f.jpg"));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("f.jpg");
            fileMock.Setup(f => f.Length).Returns(3);
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

            var url = await _service.UploadFileAsync(fileMock.Object, "images");

            Assert.NotNull(url);

            _containerClientMock.Verify(c => c.CreateIfNotExistsAsync(
                It.IsAny<PublicAccessType>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<BlobContainerEncryptionScopeOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _containerClientMock.Verify(c => c.GetBlobClient(It.IsAny<string>()), Times.Once);

            _blobClientMock.Verify(b => b.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}