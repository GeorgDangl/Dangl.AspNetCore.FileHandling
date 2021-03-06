﻿using Microsoft.Extensions.DependencyInjection;

namespace Dangl.AspNetCore.FileHandling.Azure
{
    /// <summary>
    /// Extensions with dependency injection utilities
    /// </summary>
    public static class FileHandlingExtensions
    {
        /// <summary>
        /// Adds the <see cref="IFileManager"/> as <see cref="AzureBlobFileManager"/> implementation.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="storageConnectionString"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureBlobFileManager(this IServiceCollection services, string storageConnectionString)
        {
            services.AddTransient<IFileManager>(sc => new AzureBlobFileManager(storageConnectionString));
            return services;
        }
    }
}
