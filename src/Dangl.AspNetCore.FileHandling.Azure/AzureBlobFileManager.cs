using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Dangl.Data.Shared;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// Implementation for <see cref="IFileManager"/> which uses Azure blob storage
    /// </summary>
    public class AzureBlobFileManager : IFileManager
    {
        private readonly BlobServiceClient _blobClient;
        private readonly string _accessKey;

        /// <summary>
        /// Instantiates this class with a connection to Azure blob storage
        /// </summary>
        /// <param name="storageConnectionString"></param>
        public AzureBlobFileManager(string storageConnectionString)
        {
            _blobClient = new BlobServiceClient(storageConnectionString);

            _accessKey = storageConnectionString
                .Split(';')
                .Where(s => s.StartsWith("AccountKey=", StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Substring("AccountKey=".Length))
                .FirstOrDefault();
        }

        /// <summary>
        /// Will return the file from the blob storage or
        /// a failed repository result if it does not exist
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<Stream>> GetFileAsync(string container, string fileName)
        {
            return InternalGetFileAsync(null, container, fileName);
        }

        /// <summary>
        /// Will return the file from the blob storage or
        /// a failed repository result if it does not exist
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName)
        {
            return InternalGetFileAsync(fileId, container, fileName);
        }

        private async Task<RepositoryResult<Stream>> InternalGetFileAsync(Guid? fileId, string container, string fileName)
        {
            var blobReference = fileId == null
                ? GetBlobReference(container, fileName)
                : GetBlobReference((Guid)fileId, container, fileName);
            try
            {
                var memoryStream = new MemoryStream();
                var blobResponse = await blobReference.DownloadAsync();
                await blobResponse.Value.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return RepositoryResult<Stream>.Success(memoryStream);
            }
            catch (Exception e)
            {
                return RepositoryResult<Stream>.Fail(e.ToString());
            }
        }

        /// <summary>
        /// Will save the file to Azure blob storage
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(string container, string fileName, Stream fileStream)
        {
            var blobReference = GetBlobReference(container, fileName);
            return UploadBlobReference(blobReference, fileStream);
        }

        /// <summary>
        /// Will save the file to Azure blob storage
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream)
        {
            var blobReference = GetBlobReference(fileId, container, fileName);
            return UploadBlobReference(blobReference, fileStream);
        }

        /// <summary>
        /// Saves a file in a date-hierarchical format, e.g. to {container}/2018/07/19/14/2018-07-19-14-33-32_filename.ext
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container">
        /// The container name must be at least 3 characters long and at maximum 63 characters long.
        /// It can only consist of lowercase alphanumeric characters and the '-' (dash) character. This is to enforce
        /// compatibility with Azure blob storage, if a later migration is performed to Azure.
        /// </param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(DateTime fileDate, string container, string fileName, Stream fileStream)
        {
            var blobReference = GetTimeStampedBlobReference(fileDate, container, fileName);
            return UploadBlobReference(blobReference, fileStream);
        }

        private async Task<RepositoryResult> UploadBlobReference(BlobClient blobReference, Stream fileStream)
        {
            try
            {
                await blobReference.UploadAsync(fileStream);
                return RepositoryResult.Success();
            }
            catch (Exception e)
            {
                return RepositoryResult.Fail(e.ToString());
            }
        }

        /// <summary>
        /// Creates the container if it does not yet exist in the storage account
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> EnsureContainerCreated(string container)
        {
            await _blobClient.GetBlobContainerClient(container)
                .CreateIfNotExistsAsync();
            return RepositoryResult.Success();
        }

        private BlobClient GetBlobReference(string container, string fileName)
        {
            var filePath = fileName
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var containerReference = _blobClient.GetBlobContainerClient(container);
            return containerReference.GetBlobClient(filePath);
        }

        private BlobClient GetBlobReference(Guid fileId, string container, string fileName)
        {
            var filePath = $"{fileId.ToString().ToLowerInvariant()}_{fileName}"
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var containerReference = _blobClient.GetBlobContainerClient(container);
            return containerReference.GetBlobClient(filePath);
        }

        private BlobClient GetTimeStampedBlobReference(DateTime fileDate, string container, string fileName)
        {
            var filePath = TimeStampedFilePathBuilder.GetTimeStampedFilePath(fileDate, fileName);
            var containerReference = _blobClient.GetBlobContainerClient(container);
            return containerReference.GetBlobClient(filePath);
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> DeleteFileAsync(string container, string fileName)
        {
            var blobReference = GetBlobReference(container, fileName);
            try
            {
                await blobReference.DeleteAsync();
                return RepositoryResult.Success();
            }
            catch
            {
                return RepositoryResult.Fail();
            }
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> DeleteFileAsync(Guid fileId, string container, string fileName)
        {
            var blobReference = GetBlobReference(fileId, container, fileName);
            try
            {
                await blobReference.DeleteAsync();
                return RepositoryResult.Success();
            }
            catch
            {
                return RepositoryResult.Fail();
            }
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> DeleteFileAsync(DateTime fileDate, string container, string fileName)
        {
            var blobReference = GetTimeStampedBlobReference(fileDate, container, fileName);
            try
            {
                await blobReference.DeleteAsync();
                return RepositoryResult.Success();
            }
            catch
            {
                return RepositoryResult.Fail();
            }
        }

        /// <summary>
        /// Creates a SAS upload link to allow direct blob upload
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <returns></returns>
        public Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkAsync(string container, string fileName, int validForMinutes = 5)
        {
            var filePath = fileName
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            return GetSasUploadLinkInternalAsync(filePath, container, validForMinutes);
        }

        /// <summary>
        /// Creates a SAS upload link to allow direct blob upload
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <returns></returns>
        public Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkAsync(Guid fileId, string container, string fileName, int validForMinutes = 5)
        {
            var filePath = $"{fileId.ToString().ToLowerInvariant()}_{fileName}"
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            return GetSasUploadLinkInternalAsync(filePath, container, validForMinutes);
        }

        /// <summary>
        /// Creates a SAS upload link to allow direct blob upload
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <returns></returns>
        public Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkAsync(DateTime fileDate, string container, string fileName, int validForMinutes = 5)
        {
            var filePath = TimeStampedFilePathBuilder.GetTimeStampedFilePath(fileDate, fileName);
            return GetSasUploadLinkInternalAsync(filePath, container, validForMinutes);
        }

        private Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkInternalAsync(string filePath, string container, int validForMinutes)
        {
            // Taken from the docs at
            // https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-user-delegation-sas-create-dotnet

            if (validForMinutes <= 0)
            {
                return Task.FromResult(RepositoryResult<SasUploadLink>.Fail("The validity in minutes must be greater than zero"));
            }

            var validUntil = DateTimeOffset.UtcNow.AddMinutes(validForMinutes);
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = container,
                BlobName = filePath,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = validUntil
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var key = new StorageSharedKeyCredential(_blobClient.AccountName, _accessKey);

            // Use the key to get the SAS token.
            string sasToken = sasBuilder.ToSasQueryParameters(key).ToString();

            // Construct the full URI, including the SAS token.
            UriBuilder fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = $"{_blobClient.AccountName}.blob.core.windows.net",
                Path = $"{container}/{filePath}",
                Query = sasToken
            };

            var uploadLink = new SasUploadLink
            {
                UploadLink = fullUri.ToString(),
                ValidUntil = validUntil
            };

            return Task.FromResult(RepositoryResult<SasUploadLink>.Success(uploadLink));
        }

        /// <summary>
        /// Checks if the file exists
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<bool>> CheckIfFileExistsAsync(string container, string fileName)
        {
            var blobReference = GetBlobReference(container, fileName);
            return InternalCheckIfFileExistsAsync(blobReference);
        }

        /// <summary>
        /// Checks if the file exists
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<bool>> CheckIfFileExistsAsync(Guid fileId, string container, string fileName)
        {
            var blobReference = GetBlobReference(fileId, container, fileName);
            return InternalCheckIfFileExistsAsync(blobReference);
        }

        /// <summary>
        /// Checks if the file exists
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<bool>> CheckIfFileExistsAsync(DateTime fileDate, string container, string fileName)
        {
            var blobReference = GetTimeStampedBlobReference(fileDate, container, fileName);
            return InternalCheckIfFileExistsAsync(blobReference);
        }

        private async Task<RepositoryResult<bool>> InternalCheckIfFileExistsAsync(BlobClient blobReference)
        {
            try
            {
                var blobExists = await blobReference.ExistsAsync();
                return RepositoryResult<bool>.Success(blobExists);
            }
            catch (Exception e)
            {
                return RepositoryResult<bool>.Fail(e.ToString());
            }
        }
    }
}
