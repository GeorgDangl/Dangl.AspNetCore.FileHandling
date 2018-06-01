namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Extensions for <see cref="string"/>s
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// This returns null for null input, otherwise the original string up to a max length
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string WithMaxLength(this string value, int maxLength)
        {
            if (value == null || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }
    }
}
