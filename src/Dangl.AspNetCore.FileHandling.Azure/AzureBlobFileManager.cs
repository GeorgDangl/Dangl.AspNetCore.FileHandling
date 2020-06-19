using System;
using System.IO;
using System.Threading.Tasks;
using Dangl.Data.Shared;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// Implementation for <see cref="IFileManager"/> which uses Azure blob storage
    /// </summary>
    public class AzureBlobFileManager : IFileManager
    {
        private readonly CloudBlobClient _blobClient;

        /// <summary>
        /// Instantiates this class with a connection to Azure blob storage
        /// </summary>
        /// <param name="storageConnectionString"></param>
        public AzureBlobFileManager(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            _blobClient = storageAccount.CreateCloudBlobClient();
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
                await blobReference.DownloadToStreamAsync(memoryStream);
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

        private async Task<RepositoryResult> UploadBlobReference(CloudBlockBlob blobReference, Stream fileStream)
        {
            try
            {
                await blobReference.UploadFromStreamAsync(fileStream);
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
            await _blobClient.GetContainerReference(container)
                .CreateIfNotExistsAsync();
            return RepositoryResult.Success();
        }

        private CloudBlockBlob GetBlobReference(string container, string fileName)
        {
            var filePath = fileName
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var containerReference = _blobClient.GetContainerReference(container);
            return containerReference.GetBlockBlobReference(filePath);
        }

        private CloudBlockBlob GetBlobReference(Guid fileId, string container, string fileName)
        {
            var filePath = $"{fileId.ToString().ToLowerInvariant()}_{fileName}"
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var containerReference = _blobClient.GetContainerReference(container);
            return containerReference.GetBlockBlobReference(filePath);
        }

        private CloudBlockBlob GetTimeStampedBlobReference(DateTime fileDate, string container, string fileName)
        {
            var filePath = TimeStampedFilePathBuilder.GetTimeStampedFilePath(fileDate, fileName);
            var containerReference = _blobClient.GetContainerReference(container);
            return containerReference.GetBlockBlobReference(filePath);
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
    }
}
