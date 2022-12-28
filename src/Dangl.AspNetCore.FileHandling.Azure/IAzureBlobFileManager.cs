using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Dangl.Data.Shared;
using System;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// This interface extends on <see cref="IFileManager"/>
    /// and adds some Azure Blob specific features
    /// </summary>
    public interface IAzureBlobFileManager : IFileManager
    {
        /// <summary>
        /// Creates the container if it does not yet exist in the storage account
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        Task<RepositoryResult> EnsureContainerCreatedAsync(string container);

        /// <summary>
        /// Returns the <see cref="BlobClient"/> that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        BlobClient GetBlobReference(string container, string fileName);

        /// <summary>
        /// Returns the <see cref="BlobClient"/> that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        BlobClient GetBlobReference(Guid fileId, string container, string fileName);

        /// <summary>
        /// Returns the <see cref="BlobClient"/> that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        BlobClient GetTimeStampedBlobReference(DateTime fileDate, string container, string fileName);

        /// <summary>
        /// Creates a SAS upload link to allow direct blob upload
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <returns></returns>
        Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkAsync(string container, string fileName, int validForMinutes = 5);

        /// <summary>
        /// Creates a SAS upload link to allow direct blob upload
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <returns></returns>
        Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkAsync(Guid fileId, string container, string fileName, int validForMinutes = 5);

        /// <summary>
        /// Creates a SAS upload link to allow direct blob upload
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <returns></returns>
        Task<RepositoryResult<SasUploadLink>> GetSasUploadLinkAsync(DateTime fileDate, string container, string fileName, int validForMinutes = 5);

        /// <summary>
        /// Creates a SAS download link to allow direct blob download
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <param name="friendlyFileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkAsync(string container, string fileName, int validForMinutes = 5, string friendlyFileName = null);

        /// <summary>
        /// Creates a SAS download link to allow direct blob download
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <param name="friendlyFileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkAsync(Guid fileId, string container, string fileName, int validForMinutes = 5, string friendlyFileName = null);

        /// <summary>
        /// Creates a SAS download link to allow direct blob download
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="validForMinutes"></param>
        /// <param name="friendlyFileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<SasDownloadLink>> GetSasDownloadLinkAsync(DateTime fileDate, string container, string fileName, int validForMinutes = 5, string friendlyFileName = null);

        /// <summary>
        /// Returns the <see cref="BlobProperties"/> for a specific blob instance that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<BlobProperties>> GetBlobPropertiesAsync(string container, string fileName);

        /// <summary>
        /// Returns the <see cref="BlobProperties"/> for a specific blob instance that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<BlobProperties>> GetBlobPropertiesAsync(Guid fileId, string container, string fileName);

        /// <summary>
        /// Returns the <see cref="BlobProperties"/> for a specific blob instance that matches the path
        /// constructed from the parameters.
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<BlobProperties>> GetBlobPropertiesAsync(DateTime fileDate, string container, string fileName);
    }
}
