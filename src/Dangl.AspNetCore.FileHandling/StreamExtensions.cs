using System.IO;

namespace Dangl.AspNetCore.FileHandling
{
    internal static class StreamExtensions
    {
        public static MemoryStream Copy(this Stream stream)
        {
            var memStream = new MemoryStream();
            stream.CopyToAsync(memStream);
            stream.Position = 0;
            memStream.Position = 0;
            return memStream;
        }
    }
}
