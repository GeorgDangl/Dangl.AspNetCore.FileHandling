using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Tests
{
    public class InMemoryFileManagerTests : IDisposable
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

        [Fact]
        public async Task CanAccessSameFileMultipleTimesEvenAfterFirstRetrivedOneWasDisposed_WithFileId()
        {
            var fileId = Guid.NewGuid();

            var inMemoryFileManager = new InMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync(fileId, "my-container", "file.bin", originalMemStream);
            }

            Assert.Single(InMemoryFileManager.SavedFiles);

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
            var inMemoryFileManager = new InMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync("my-container", "file.bin", originalMemStream);
            }

            Assert.Single(InMemoryFileManager.SavedFiles);

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
            var inMemoryFileManager = new InMemoryFileManager();
            using (var originalMemStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }))
            {
                await inMemoryFileManager.SaveFileAsync(Guid.NewGuid(), "my-container", "file.bin", originalMemStream);
            }

            Assert.Single(InMemoryFileManager.SavedFiles);
            InMemoryFileManager.ClearFiles();
            Assert.Empty(InMemoryFileManager.SavedFiles);
        }

        [Fact]
        public void TwoInstancesShareData()
        {
            var firstManager = new InMemoryFileManager();
            var secondManager = new InMemoryFileManager();

            Assert.Empty(InMemoryFileManager.SavedFiles);
            Assert.Empty(InMemoryFileManager.SavedFiles);

            firstManager.SaveFileAsync("my-container", "file.bin", new MemoryStream());

            Assert.Single(InMemoryFileManager.SavedFiles);
            Assert.Single(InMemoryFileManager.SavedFiles);
        }

        public void Dispose()
        {
            InMemoryFileManager.ClearFiles();
        }
    }
}
