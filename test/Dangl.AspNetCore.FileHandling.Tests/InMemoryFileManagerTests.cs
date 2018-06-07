using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Tests
{
    public class InMemoryFileManagerTests
    {
        [Fact]
        public async Task CanAccessSavedStreamEvenWhenOriginalIsDisposed()
        {
            var inMemoryFileManager = new InMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync(Guid.NewGuid(), "my-container", "file.bin", originalMemStream);
            }
            var savedFile = InMemoryFileManager.SavedFiles.Single();
            Assert.Equal(5, savedFile.FileStream.Length); // If it can be accessed, everything's fine
        }
    }
}
