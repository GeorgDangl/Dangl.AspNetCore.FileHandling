namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Defaults used in this package
    /// </summary>
    public static class FileHandlerDefaults
    {
        /// <summary>
        /// The max length of a file container name
        /// </summary>
        public const int FILE_CONTAINER_NAME_MAX_LENGTH = 63;

        /// <summary>
        /// The min length of a file container name
        /// </summary>
        public const int FILE_CONTAINER_NAME_MIN_LENGTH = 3;

        /// <summary>
        /// The max length of the file path
        /// </summary>
        public const int FILE_PATH_MAX_LENGTH = 1024;

        /// <summary>
        /// Regular expression defining valid file container names
        /// </summary>
        public const string FILE_CONTAINER_NAME_ALLOWED_REGEX = "^[a-z-]+$";
    }
}
