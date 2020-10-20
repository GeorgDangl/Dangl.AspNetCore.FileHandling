using System;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// This class represents an Azure Blob SAS url, which can be used to directly allow a client to upload a file
    /// directly to Azure without having to proxy it through the actual service
    /// </summary>
    public class SasUploadLink
    {
        /// <summary>
        /// The Azure Blob upload link
        /// </summary>
        public string UploadLink { get; set; }

        /// <summary>
        /// When the link expires
        /// </summary>
        public DateTimeOffset ValidUntil { get; set; }
    }
}
