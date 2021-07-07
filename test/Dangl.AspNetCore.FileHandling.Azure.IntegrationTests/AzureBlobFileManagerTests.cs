using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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
            var blobFileManager = new AzureBlobFileManager(connectionString);

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
    }
}
