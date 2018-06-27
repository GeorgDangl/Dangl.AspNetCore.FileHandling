﻿using System;
using System.IO;
using System.Threading.Tasks;
using Dangl.Data.Shared;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

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
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName)
        {
            var blobReference = GetBlobReference(fileId, container, fileName);
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
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream)
        {
            var blobReference = GetBlobReference(fileId, container, fileName);
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

        private CloudBlockBlob GetBlobReference(Guid fileId, string container, string fileName)
        {
            var filePath = $"{fileId.ToString().ToLowerInvariant()}_{fileName}"
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var containerReference = _blobClient.GetContainerReference(container);
            return containerReference.GetBlockBlobReference(filePath);
        }
    }
}