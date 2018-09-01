using Dangl.Data.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Implementation for <see cref="IFileManager"/> which uses the disk
    /// </summary>
    public class DiskFileManager : IFileManager
    {
        private readonly string _rootFolder;

        /// <summary>
        /// Instantiates this class with a root folder on disk
        /// </summary>
        /// <param name="rootFolder"></param>
        public DiskFileManager(string rootFolder)
        {
            _rootFolder = rootFolder ?? throw new ArgumentNullException(nameof(rootFolder));
        }

        /// <summary>
        /// Will return the file on disk or a failed repository result for errors or if the file
        /// can not be found
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<Stream>> GetFileAsync(string container, string fileName)
        {
            return InternalGetFileAsync(null, container, fileName);
        }

        /// <summary>
        /// Will return the file on disk or a failed repository result for errors or if the file
        /// can not be found
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName)
        {
            return InternalGetFileAsync(fileId, container, fileName);
        }

        private Task<RepositoryResult<Stream>> InternalGetFileAsync(Guid? fileId, string container, string fileName)
        {
            var fileSavePath = GetFilePath(fileId, container, fileName);

            try
            {
                if (!File.Exists(fileSavePath))
                {
                    return Task.FromResult(RepositoryResult<Stream>.Fail("The file does not exist"));
                }

                var fileStream = File.Open(fileSavePath, FileMode.Open);

                return Task.FromResult(RepositoryResult<Stream>.Success(fileStream));
            }
            catch (Exception e)
            {
                return Task.FromResult(RepositoryResult<Stream>.Fail(e.ToString()));
            }
        }

        /// <summary>
        /// Will save the file on disk, relative to the root folder in a folder
        /// matching the container name
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(string container, string fileName, Stream fileStream)
        {
            var fileSavePath = GetFilePath(null, container, fileName);
            return SaveFileToDiskAsync(fileSavePath, fileStream);
        }

        /// <summary>
        /// Will save the file on disk, relative to the root folder in a folder
        /// matching the container name
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream)
        {
            var fileSavePath = GetFilePath(fileId, container, fileName);
            return SaveFileToDiskAsync(fileSavePath, fileStream);
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
            var timeStampedRelativePath = TimeStampedFilePathBuilder.GetTimeStampedFilePath(fileDate, fileName);
            var fileSavePath = Path.Combine(_rootFolder, container, timeStampedRelativePath);
            return SaveFileToDiskAsync(fileSavePath, fileStream);
        }

        private async Task<RepositoryResult> SaveFileToDiskAsync(string absoluteFilePath, Stream fileStream)
        {
            try
            {
                var path = Path.GetDirectoryName(absoluteFilePath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (var fs = File.Create(absoluteFilePath))
                {
                    await fileStream.CopyToAsync(fs).ConfigureAwait(false);
                }

                return RepositoryResult.Success();
            }
            catch (Exception e)
            {
                return RepositoryResult.Fail(e.ToString());
            }
        }

        /// <summary>
        /// This will return the full, absolute file path of a file to save. It will be truncated to a max length of 1024 characters to be compatible with
        /// Azure storage restrictions if a later migration is performed to Azure.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container">
        /// The container name must be at least 3 characters long and at maximum 63 characters long.
        /// It can only consist of lowercase alphanumeric characters and the '-' (dash) character. This is to enforce
        /// compatibility with Azure blob storage, if a later migration is performed to Azure.
        /// </param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFilePath(Guid? fileId, string container, string fileName)
        {
            var relativeFilePath = fileId == null
                ? RelativeFilePathBuilder.GetRelativeFilePath(container, fileName)
                : RelativeFilePathBuilder.GetRelativeFilePath((Guid)fileId, container, fileName);
            var fullFilePath = Path.Combine(_rootFolder, relativeFilePath);
            return fullFilePath;
        }
    }
}
