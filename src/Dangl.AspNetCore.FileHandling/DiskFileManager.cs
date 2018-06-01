using Dangl.Data.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling
{
    // TODO ADD EXTENSION FOR DEPENDENCY INJECTION
    // TODO ADD IN MEMORY FILE MANAGER

    public class DiskFileManager : IFileManager
    {
        private readonly string _rootFolder;

        public DiskFileManager(string rootFolder)
        {
            _rootFolder = rootFolder ?? throw new ArgumentNullException(nameof(rootFolder));
        }

        public Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName)
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

        public async Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream)
        {
            var fileSavePath = GetFilePath(fileId, container, fileName);

            try
            {
                var path = Path.GetDirectoryName(fileSavePath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                using (var fs = File.Create(fileSavePath))
                {
                    await fileStream.CopyToAsync(fs);
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
        public string GetFilePath(Guid fileId, string container, string fileName)
        {
            var relativeFilePath = RelativeFilePathBuilder.GetRelativeFilePath(fileId, container, fileName);
            var fullFilePath = Path.Combine(_rootFolder, relativeFilePath);
            return fullFilePath;
        }
    }
}
