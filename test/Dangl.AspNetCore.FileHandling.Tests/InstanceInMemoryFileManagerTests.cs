using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Tests
{
    public class InstanceInMemoryFileManagerTests
    {
        [Fact]
        public async Task CanAccessSavedStreamEvenWhenOriginalIsDisposed()
        {
            var inMemoryFileManager = new InstanceInMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync(Guid.NewGuid(), "my-container", "file.bin", originalMemStream);
            }
            var savedFile = inMemoryFileManager.SavedFiles.Single();
            Assert.Equal(5, savedFile.FileStream.Length); // If it can be accessed, everything's fine
        }

        [Fact]
        public async Task CanAccessSameFileMultipleTimesEvenAfterFirstRetrivedOneWasDisposed_WithFileId()
        {
            var fileId = Guid.NewGuid();

            var inMemoryFileManager = new InstanceInMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync(fileId, "my-container", "file.bin", originalMemStream);
            }

            Assert.Single(inMemoryFileManager.SavedFiles);

            using (var accessedStream = (await inMemoryFileManager.GetFileAsync(fileId, "my-container", "file.bin")).Value)
            {
                Assert.Equal(5, accessedStream.Length); // If it can be accessed, everything's fine
            }

            using (var accessedStream = (await inMemoryFileManager.GetFileAsync(fileId, "my-container", "file.bin")).Value)
            {
                Assert.Equal(5, accessedStream.Length); // If it can be accessed, everything's fine
            }
        }

        [Fact]
        public async Task CanAccessSameFileMultipleTimesEvenAfterFirstRetrivedOneWasDisposed_WithoutFileId()
        {
            var inMemoryFileManager = new InstanceInMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync("my-container", "file.bin", originalMemStream);
            }

            Assert.Single(inMemoryFileManager.SavedFiles);

            using (var accessedStream = (await inMemoryFileManager.GetFileAsync("my-container", "file.bin")).Value)
            {
                Assert.Equal(5, accessedStream.Length); // If it can be accessed, everything's fine
            }

            using (var accessedStream = (await inMemoryFileManager.GetFileAsync("my-container", "file.bin")).Value)
            {
                Assert.Equal(5, accessedStream.Length); // If it can be accessed, everything's fine
            }
        }

        [Fact]
        public async Task CanClearFiles()
        {
            var inMemoryFileManager = new InstanceInMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync(Guid.NewGuid(), "my-container", "file.bin", originalMemStream);
            }

            Assert.Single(inMemoryFileManager.SavedFiles);
            inMemoryFileManager.ClearFiles();
            Assert.Empty(inMemoryFileManager.SavedFiles);
        }

        [Fact]
        public void TwoInstancesDontShareData()
        {
            var firstManager = new InstanceInMemoryFileManager();
            var secondManager = new InstanceInMemoryFileManager();

            Assert.Empty(firstManager.SavedFiles);
            Assert.Empty(secondManager.SavedFiles);

            firstManager.SaveFileAsync("my-container", "file.bin", new MemoryStream());

            Assert.Single(firstManager.SavedFiles);
            Assert.Empty(secondManager.SavedFiles);
        }
    }
}
