using Dangl.Data.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Interface for file handling
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Will return the file or a failed repository result for errors or if the file
        /// can not be found
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<Stream>> GetFileAsync(string container, string fileName);

        /// <summary>
        /// Will return the file or a failed repository result for errors or if the file
        /// can not be found
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName);

        /// <summary>
        /// Saves a file
        /// </summary>
        /// <param name="container">
        /// The container name must be at least 3 characters long and at maximum 63 characters long.
        /// It can only consist of lowercase alphanumeric characters and the '-' (dash) character. This is to enforce
        /// compatibility with Azure blob storage, if a later migration is performed to Azure.
        /// </param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        Task<RepositoryResult> SaveFileAsync(string container, string fileName, Stream fileStream);

        /// <summary>
        /// Saves a file
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container">
        /// The container name must be at least 3 characters long and at maximum 63 characters long.
        /// It can only consist of lowercase alphanumeric characters and the '-' (dash) character. This is to enforce
        /// compatibility with Azure blob storage, if a later migration is performed to Azure.
        /// </param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream);

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
        Task<RepositoryResult> SaveFileAsync(DateTime fileDate, string container, string fileName, Stream fileStream);
    }
}
