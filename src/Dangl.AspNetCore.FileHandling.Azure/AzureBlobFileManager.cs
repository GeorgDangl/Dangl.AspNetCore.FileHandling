using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Dangl.Data.Shared;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// Implementation for <see cref="IFileManager"/> which uses Azure blob storage
    /// </summary>
    public class AzureBlobFileManager : IAzureBlobFileManager
    {
        private readonly BlobServiceClient _blobClient;
        private readonly string _accessKey;

        /// <summary>
        /// Instantiates this class with a connection to Azure blob storage
        /// </summary>
        /// <param name="storageConnectionString"></param>
        /// <param name="blobServiceClient"></param>
        public AzureBlobFileManager(string storageConnectionString,
            BlobServiceClient blobServiceClient)
        {
            _blobClient = blobServiceClient;

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

        /// <summary>
        /// Returns the <see cref="BlobClient"/> that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public BlobClient GetBlobReference(string container, string fileName)
        {
            var filePath = fileName
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var containerReference = _blobClient.GetBlobContainerClient(container);
            return containerReference.GetBlobClient(filePath);
        }

        /// <summary>
        /// Returns the <see cref="BlobClient"/> that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public BlobClient GetBlobReference(Guid fileId, string container, string fileName)
        {
            var filePath = AzureBlobFilePathBuilder.GetBlobReferenceWithFileId(fileId, fileName);
            var containerReference = _blobClient.GetBlobContainerClient(container);
            return containerReference.GetBlobClient(filePath);
        }

        /// <summary>
        /// Returns the <see cref="BlobClient"/> that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public BlobClient GetTimeStampedBlobReference(DateTime fileDate, string container, string fileName)
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
            var filePath = AzureBlobFilePathBuilder.GetBlobReferenceWithFileId(fileId, fileName);
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

        private async Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkInternalAsync(string filePath, string container, int validForMinutes)
        {
            var sasUploadLink = await GeSasLinkInternalAsync(filePath, container, validForMinutes, BlobSasPermissions.Create | BlobSasPermissions.Write, null);
            if (!sasUploadLink.IsSuccess)
            {
                return RepositoryResult<SasUploadLink>.Fail(sasUploadLink.ErrorMessage);
            }

            return RepositoryResult<SasUploadLink>.Success(new SasUploadLink
            {
                UploadLink = sasUploadLink.Value.link,
                ValidUntil = sasUploadLink.Value.validUntil
            });
        }

        /// <summary>
        /// Creates a SAS download link to allow direct blob download
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <param name="friendlyFileName">A filename to optionally use for the SAS download</param>
        /// <returns></returns>
        public Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkAsync(string container, string fileName, int validForMinutes = 5, string friendlyFileName = null)
        {
            var filePath = fileName
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            return GetSasDownloadLinkInternalAsync(filePath, container, validForMinutes, friendlyFileName);
        }

        /// <summary>
        /// Creates a SAS download link to allow direct blob download
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <param name="friendlyFileName">A filename to optionally use for the SAS download</param>
        /// <returns></returns>
        public Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkAsync(Guid fileId, string container, string fileName, int validForMinutes = 5, string friendlyFileName = null)
        {
            var filePath = AzureBlobFilePathBuilder.GetBlobReferenceWithFileId(fileId, fileName);
            return GetSasDownloadLinkInternalAsync(filePath, container, validForMinutes, friendlyFileName);
        }

        /// <summary>
        /// Creates a SAS download link to allow direct blob download
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <param name="friendlyFileName">A filename to optionally use for the SAS download</param>
        /// <returns></returns>
        public Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkAsync(DateTime fileDate, string container, string fileName, int validForMinutes = 5, string friendlyFileName = null)
        {
            var filePath = TimeStampedFilePathBuilder.GetTimeStampedFilePath(fileDate, fileName);
            return GetSasDownloadLinkInternalAsync(filePath, container, validForMinutes, friendlyFileName);
        }

        private async Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkInternalAsync(string filePath, string container, int validForMinutes, string friendlyFileName)
        {
            var sasUploadLink = await GeSasLinkInternalAsync(filePath, container, validForMinutes, BlobSasPermissions.Read, friendlyFileName);
            if (!sasUploadLink.IsSuccess)
            {
                return RepositoryResult<SasDownloadLink>.Fail(sasUploadLink.ErrorMessage);
            }

            return RepositoryResult<SasDownloadLink>.Success(new SasDownloadLink
            {
                DownloadLink = sasUploadLink.Value.link,
                ValidUntil = sasUploadLink.Value.validUntil
            });
        }

        private Task<RepositoryResult<(string link, DateTimeOffset validUntil)>> GeSasLinkInternalAsync(string filePath, string container, int validForMinutes, BlobSasPermissions permissions, string friendlyFileName)
        {
            // Taken from the docs at
            // https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-user-delegation-sas-create-dotnet

            if (validForMinutes <= 0)
            {
                return Task.FromResult(RepositoryResult<(string, DateTimeOffset)>.Fail("The validity in minutes must be greater than zero"));
            }

            var validUntil = DateTimeOffset.UtcNow.AddMinutes(validForMinutes);
            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = container,
                BlobName = filePath,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = validUntil,
            };

            if (!string.IsNullOrWhiteSpace(friendlyFileName))
            {
                sasBuilder.ContentDisposition = new ContentDisposition
                {
                    DispositionType = "attachment",
                    FileName = friendlyFileName
                }.ToString();
            }
            else
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    sasBuilder.ContentDisposition = new ContentDisposition
                    {
                        DispositionType = "attachment",
                        FileName = fileName
                    }.ToString();
                }
                catch { /* We're ignoring the case where a non-valid file name was given */ }
            }

            sasBuilder.SetPermissions(permissions);

            var key = new StorageSharedKeyCredential(_blobClient.AccountName, _accessKey);

            // Construct the full URI, including the SAS token.
            var blobReference = GetBlobReference(container, filePath);
            var blobUri = new BlobUriBuilder(blobReference.Uri)
            {
                // Use the key to get the SAS token.
                Sas = sasBuilder.ToSasQueryParameters(key),
                BlobContainerName = container,
                BlobName = filePath,
            }
            .ToUri();

            return Task.FromResult(RepositoryResult<(string, DateTimeOffset)>.Success((blobUri.ToString(), validUntil)));
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

        /// <summary>
        /// Returns the <see cref="BlobProperties"/> for a specific blob instance that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<BlobProperties>> GetBlobPropertiesAsync(string container, string fileName)
        {
            var blob = GetBlobReference(container, fileName);
            return GetBlobPropertiesInternalAsync(blob);
        }

        /// <summary>
        /// Returns the <see cref="BlobProperties"/> for a specific blob instance that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<BlobProperties>> GetBlobPropertiesAsync(Guid fileId, string container, string fileName)
        {
            var blob = GetBlobReference(fileId, container, fileName);
            return GetBlobPropertiesInternalAsync(blob);
        }

        /// <summary>
        /// Returns the <see cref="BlobProperties"/> for a specific blob instance that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<BlobProperties>> GetBlobPropertiesAsync(DateTime fileDate, string container, string fileName)
        {
            var blob = GetTimeStampedBlobReference(fileDate, container, fileName);
            return GetBlobPropertiesInternalAsync(blob);
        }

        private async Task<RepositoryResult<BlobProperties>> GetBlobPropertiesInternalAsync(BlobClient blob)
        {
            var propertiesResponse = await blob.GetPropertiesAsync();
            return propertiesResponse.Value;
        }
    }
}
