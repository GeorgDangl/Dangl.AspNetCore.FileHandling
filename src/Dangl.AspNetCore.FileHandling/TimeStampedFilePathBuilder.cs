using System;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Extensions to get a timestamped file path
    /// </summary>
    public static class TimeStampedFilePathBuilder
    {
        /// <summary>
        /// This will return a date hierarchical filename, e.g. 2018/07/19/14/2018-07-19-14-33-32_filename.ext
        /// </summary>
        /// <param name="fileDate"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetTimeStampedFilePath(DateTime fileDate, string fileName)
        {
            var fileTimestamp = $"{fileDate:yyyy-MM-dd-HH-mm-ss}";
            var filePath = $"{fileDate:yyyy}/{fileDate:MM}/{fileDate:dd}/{fileDate:HH}/{fileTimestamp}_{fileName}".WithMaxLength(FileHandlerDefaults.FILE_PATH_MAX_LENGTH);
            return filePath;
        }
    }
}
