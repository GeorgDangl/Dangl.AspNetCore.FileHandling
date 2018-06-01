using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Build class for relative file paths
    /// </summary>
    public static class RelativeFilePathBuilder
    {
        /// <summary>
        /// This will return the relative file path of a file to save. It will be truncated to a max length of 1024 characters to be compatible with
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
        public static string GetRelativeFilePath(Guid fileId, string container, string fileName)
        {
            if (string.IsNullOrWhiteSpace(container))
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (!ContainerNameIsValid(container))
            {
                throw new ArgumentException($"The {nameof(container)} may only contain lowercase alphanumeric characters or the dash '-' char.", nameof(container));
            }

            var firstSegment = fileId.ToString().Substring(0, 2);
            var secondSegment = fileId.ToString().Substring(2, 2);
            var fileSaveName = string.IsNullOrWhiteSpace(fileName)
                ? fileId.ToString()
                : $"{fileId}_{fileName}";
            fileSaveName = fileSaveName.WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            var relativeFilePath = Path.Combine(container, firstSegment, secondSegment, fileSaveName);
            return relativeFilePath;
        }

        private static bool ContainerNameIsValid(string container)
        {
            var isValid = Regex.IsMatch(container, FileHandlerDefaults.FILE_CONTAINER_NAME_ALLOWED_REGEX, RegexOptions.Compiled);
            return isValid
                   && container.Length <= FileHandlerDefaults.FILE_CONTAINER_NAME_MAX_LENGTH
                   && container.Length >= FileHandlerDefaults.FILE_CONTAINER_NAME_MIN_LENGTH;
        }
    }
}
