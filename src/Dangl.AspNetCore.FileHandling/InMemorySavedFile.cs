using System;
using System.IO;

#pragma warning disable CS1591

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Represents a file cached in memory
    /// </summary>
    public class InMemorySavedFile
    {
        public Guid FileId { get; set; }

        public string Container { get; set; }

        public string FileName { get; set; }

        public Stream FileStream { get; set; }
    }
}
