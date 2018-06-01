using Dangl.Data.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling
{
    public interface IFileManager
    {
        Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName);

        /// <summary>
        /// 
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
    }
}
