using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static List<InMemorySavedFile> SavedFiles { get; } = new List<InMemorySavedFile>();

        /// <summary>
        /// Removes all cached files
        /// </summary>
        public static void ClearFiles()
        {
            SavedFiles.Clear();
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
            var file = SavedFiles
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
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public Task<RepositoryResult> SaveFileAsync(Guid fileId, string container, string fileName, Stream fileStream)
        {
            SavedFiles.Add(new InMemorySavedFile
            {
                FileId = fileId,
                Container = container,
                FileName = fileName,
                FileStream = fileStream
            });

            return Task.FromResult(RepositoryResult.Success());
        }
    }
}
