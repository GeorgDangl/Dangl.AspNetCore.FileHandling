﻿using Dangl.Data.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// In memory implementation for testing. This implementation will keep all files
    /// in a static list.
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
                // We're copying and returning a new stream, since the caller
                // might dispose the returned value, but we need to ensure that
                // we can always again return a given file
                return Task.FromResult(RepositoryResult<Stream>.Success(file.FileStream.Copy()));
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
                // We're copying and returning a new stream, since the caller
                // might dispose the returned value, but we need to ensure that
                // we can always again return a given file
                return Task.FromResult(RepositoryResult<Stream>.Success(file.FileStream.Copy()));
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
            await fileStream.CopyToAsync(copiedMemoryStream).ConfigureAwait(false);
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
            await fileStream.CopyToAsync(copiedMemoryStream).ConfigureAwait(false);
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

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult> DeleteFileAsync(string container, string fileName)
        {
            var file = _savedFiles.FirstOrDefault(f => f.Container == container
                && f.FileName == fileName);
            if (file != null)
            {
                _savedFiles.Remove(file);
            }
            return Task.FromResult(RepositoryResult.Success());
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult> DeleteFileAsync(Guid fileId, string container, string fileName)
        {
            var file = _savedFiles.FirstOrDefault(f => f.FileId == fileId
                && f.Container == container
                && f.FileName == fileName);
            if (file != null)
            {
                _savedFiles.Remove(file);
            }
            return Task.FromResult(RepositoryResult.Success());
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult> DeleteFileAsync(DateTime fileDate, string container, string fileName)
        {
            var file = _savedFiles.FirstOrDefault(f => f.Container == container
                && f.FileName == fileName);
            if (file != null)
            {
                _savedFiles.Remove(file);
            }
            return Task.FromResult(RepositoryResult.Success());
        }

        /// <summary>
        /// Checks if the file exists
        /// </summary>
        /// <param name="container"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Task<RepositoryResult<bool>> CheckIfFileExistsAsync(string container, string fileName)
        {
            var fileExists = _savedFiles
                .Any(f => f.Container == container
                    && f.FileName == fileName);
            return Task.FromResult(RepositoryResult<bool>.Success(fileExists));
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
            var fileExists = _savedFiles
                .Any(f => f.Container == container
                    && f.FileName == fileName
                    && f.FileId == fileId);
            return Task.FromResult(RepositoryResult<bool>.Success(fileExists));
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
            var fileExists = _savedFiles
                .Any(f => f.Container == container
                    && f.FileName == fileName);
            return Task.FromResult(RepositoryResult<bool>.Success(fileExists));
        }
    }
}
