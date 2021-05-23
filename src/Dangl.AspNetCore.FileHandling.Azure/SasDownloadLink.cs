using System;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// This class represents an Azure Blob SAS url, which can be used to directly allow a client to download a file
    /// directly from Azure without having to proxy it through the actual service
    /// </summary>
    public class SasDownloadLink
    {
        /// <summary>
        /// The Azure Blob download link
        /// </summary>
        public string DownloadLink { get; set; }

        /// <summary>
        /// When the link expires
        /// </summary>
        public DateTimeOffset ValidUntil { get; set; }
    }
}
