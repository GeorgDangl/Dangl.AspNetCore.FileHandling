using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dangl.Data.Shared;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// In memory implementation for testing
    /// </summary>
    public class InMemoryFileManager : IFileManager
    {
        /// <summary>
        /// Gives access to all cached files
        /// </summary>
        public static IReadOnlyList<InMemorySavedFile> SavedFiles => _savedFiles.AsReadOnly();
        private static List<InMemorySavedFile> _savedFiles = new List<InMemorySavedFile>();


        /// <summary>
        /// Removes all cached files
        /// </summary>
        public static void ClearFiles()
        {
            _savedFiles.ForEach(s => s.FileStream.Dispose());
            _savedFiles.Clear();
        }

        /// <summary>
        /// Returns a cached file
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<Stream>> GetFileAsync(string container, string fileName)
        {
            var file = _savedFiles
                .Find(f => f.Container == container
                    && f.FileName == fileName);

            if (file != null)
            {
                return Task.FromResult(RepositoryResult<Stream>.Success(file.FileStream));
            }

            return Task.FromResult(RepositoryResult<Stream>.Fail("File not found"));
        }

        /// <summary>
        /// Returns a cached file
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<Stream>> GetFileAsync(Guid fileId, string container, string fileName)
        {
            var file = _savedFiles
                .Find(f => f.FileId == fileId
                    && f.Container == container
                    && f.FileName == fileName);

            if (file != null)
            {
                return Task.FromResult(RepositoryResult<Stream>.Success(file.FileStream));
            }

            return Task.FromResult(RepositoryResult<Stream>.Fail("File not found"));
        }

        /// <summary>
        /// Caches a file to memory
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(string container, string fileName, Stream fileStream)
        {
            return SaveFileAsync(Guid.NewGuid(), container, fileName, fileStream);
        }

        /// <summary>
        /// Caches a file to memory
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream)
        {
            // Copying it internally because the original stream is likely to be disposed
            var copiedMemoryStream = new MemoryStream();
            await fileStream.CopyToAsync(copiedMemoryStream);
            copiedMemoryStream.Position = 0;

            _savedFiles.Add(new InMemorySavedFile
            {
                FileId = fileId,
                Container = container,
                FileName = fileName,
                FileStream = copiedMemoryStream
            });

            return RepositoryResult.Success();
        }

        /// <summary>
        /// Caches a file to memory
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public async Task<RepositoryResult> SaveFileAsync(DateTime fileDate, string container, string fileName, Stream fileStream)
        {
            // Copying it internally because the original stream is likely to be disposed
            var copiedMemoryStream = new MemoryStream();
            await fileStream.CopyToAsync(copiedMemoryStream);
            copiedMemoryStream.Position = 0;

            _savedFiles.Add(new InMemorySavedFile
            {
                FileId = Guid.NewGuid(),
                Container = container,
                FileName = fileName,
                FileStream = copiedMemoryStream
            });

            return RepositoryResult.Success();
        }
    }
}
