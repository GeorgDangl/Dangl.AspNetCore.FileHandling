using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Dangl.AspNetCore.FileHandling
{
    public static class RelativeFilePathBuilder
    {
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
            var containerNameRegex = "^[a-z-]+$";
            var isValid = Regex.IsMatch(container, containerNameRegex, RegexOptions.Compiled);
            return isValid
                   && container.Length <= FileHandlerDefaults.FILE_CONTAINER_NAME_MAX_LENGTH
                   && container.Length > FileHandlerDefaults.FILE_CONTAINER_NAME_MIN_LENGTH;
        }
    }
}
