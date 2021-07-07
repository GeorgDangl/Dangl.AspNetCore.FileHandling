using System;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// This static utility class provides methods to build file paths
    /// used in Azure Blob Storage
    /// </summary>
    public static class AzureBlobFilePathBuilder
    {
        /// <summary>
        /// This returns a string in the form $"{fileId.ToString().ToLowerInvariant()}_{fileName}"
        /// with a max length of 1024
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetBlobReferenceWithFileId(Guid fileId, string fileName)
        {
            var filePath = $"{fileId.ToString().ToLowerInvariant()}_{fileName}"
                    .WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            return filePath;
        }
    }
}
