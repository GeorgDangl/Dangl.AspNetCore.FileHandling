using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Azure.IntegrationTests
{
    public class AzureBlobFileManagerTests : IClassFixture<DockerTestUtilities>
    {
        private readonly DockerTestUtilities _dockerTestUtilities;

        public AzureBlobFileManagerTests(DockerTestUtilities dockerTestUtilities)
        {
            _dockerTestUtilities = dockerTestUtilities;
        }

        [Fact]
        public async Task SasUploadWorkflow()
        {
            // In case this fails with an error message referencing an invalid header value
            // for 'x-ms-version', please ensure to manually pull the latest Azurite Docker
            // image.
            // The CI handles this automatically

            var connectionString = _dockerTestUtilities.GetBlobConnectionString();
            var blobFileManager = new AzureBlobFileManager(connectionString, new BlobServiceClient(connectionString));

            var containerName = "test-files";
            await blobFileManager.EnsureContainerCreated(containerName);
            var fileName = "sas-upload.txt";

            // Get the SAS token
            var sasUploadUrl = await blobFileManager.GetSasUploadLinkAsync(containerName, fileName);
            Assert.True(sasUploadUrl.IsSuccess);

            // Ensure the file is not yet recognized as existing
            var fileAlreadyExists = await blobFileManager.CheckIfFileExistsAsync(containerName, fileName);
            Assert.True(fileAlreadyExists.IsSuccess);
            Assert.False(fileAlreadyExists.Value);

            // Manually upload the file via the SAS link
            using var httpClient = new HttpClient();
            var fileData = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6 });
            var streamContent = new StreamContent(fileData);
            streamContent.Headers.Add("x-ms-blob-type", "BlockBlob");
            var sasUploadResponse = await httpClient.PutAsync(sasUploadUrl.Value.UploadLink, streamContent);
            Assert.True(sasUploadResponse.IsSuccessStatusCode);

            // The file should now be detected as being available
            fileAlreadyExists = await blobFileManager.CheckIfFileExistsAsync(containerName, fileName);
            Assert.True(fileAlreadyExists.IsSuccess);
            Assert.True(fileAlreadyExists.Value);

            // And we should be able to download the file
            var fileGetResult = await blobFileManager.GetFileAsync(containerName, fileName);
            Assert.True(fileGetResult.IsSuccess);
            Assert.Equal(6, fileGetResult.Value.Length);
        }

        [Theory]
        [InlineData("file.dat", "file.dat")]
        [InlineData("😀.dat", "\"=?utf-8?B?8J+YgC5kYXQ=?=\"")]
        public async Task CanDownloadSasFileWithCustomFriendlyFileName(string givenFriendlyName, string expectedFriendlyName)
        {
            // Note for this test: Azure itself doesn't yet support setting the 'Content-Dispostion' header, so this is more
            // of an unit test. See here:
            // https://github.com/Azure/Azurite/issues/470

            var connectionString = _dockerTestUtilities.GetBlobConnectionString();
            var blobFileManager = new AzureBlobFileManager(connectionString, new BlobServiceClient(connectionString));

            var containerName = "test-files";
            await blobFileManager.EnsureContainerCreated(containerName);
            var fileName = "sas-download.txt";

            var fileId = Guid.NewGuid();

            var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            var fileSaveResult = await blobFileManager.SaveFileAsync(fileId, containerName, fileName, fileStream);
            Assert.True(fileSaveResult.IsSuccess);

            var regularSasDownloadLink = await blobFileManager.GetSasDownloadLinkAsync(fileId, containerName, fileName);
            Assert.True(regularSasDownloadLink.IsSuccess);

            using var httpClient = new HttpClient();

            var regularSasResponse = await httpClient.GetAsync(regularSasDownloadLink.Value.DownloadLink);
            Assert.True(regularSasResponse.IsSuccessStatusCode);
            var contentDispositionValue = HttpUtility.ParseQueryString(new Uri(regularSasDownloadLink.Value.DownloadLink)
                .Query)
                .Get("rscd");

            Assert.Equal($"attachment; filename={fileId}_{fileName}", contentDispositionValue);

            var friendlyNameSasDownloadLink = await blobFileManager.GetSasDownloadLinkAsync(fileId, containerName, fileName, friendlyFileName: givenFriendlyName);
            Assert.True(friendlyNameSasDownloadLink.IsSuccess);

            var friendlySasResponse = await httpClient.GetAsync(friendlyNameSasDownloadLink.Value.DownloadLink);
            Assert.True(friendlySasResponse.IsSuccessStatusCode);
            contentDispositionValue = HttpUtility.ParseQueryString(new Uri(friendlyNameSasDownloadLink.Value.DownloadLink)
                .Query)
                .Get("rscd");

            Assert.Equal($"attachment; filename={expectedFriendlyName}", contentDispositionValue);
        }
    }
}
