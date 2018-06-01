using Microsoft.Extensions.DependencyInjection;

namespace Dangl.AspNetCore.FileHandling
{
    /// <summary>
    /// Extensions with dependency injection utilities
    /// </summary>
    public static class FileHandlingExtensions
    {
        /// <summary>
        /// Adds the <see cref="InMemoryFileManager"/> as singleton. This should
        /// only be used for testing.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddInMemoryFileManager(this IServiceCollection services)
        {
            services.AddSingleton<IFileManager, InMemoryFileManager>();
            return services;
        }

        /// <summary>
        /// Adds the <see cref="IFileManager"/> as <see cref="DiskFileManager"/> implementation.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        public static IServiceCollection AddDiskFileManager(this IServiceCollection services, string rootFolder)
        {
            services.AddTransient<IFileManager>(sc => new DiskFileManager(rootFolder));
            return services;
        }
    }
}
