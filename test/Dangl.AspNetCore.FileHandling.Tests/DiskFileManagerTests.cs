﻿using System;
using Xunit;

namespace Dangl.AspNetCore.FileHandling.Tests
{
    public class DiskFileManagerTests
    {
        [Fact]
        public void ArgumentNullExceptionForNullRootFolder()
        {
            Assert.Throws<ArgumentNullException>("rootFolder", () => new DiskFileManager(null));
        }

        public class GetFilePath
        {
            private readonly string _rootFolder = @"Y:\Files";
            private readonly Guid _fileId = new Guid("c3b836ef-ec43-4ac2-bba1-f477db3f480d");
            private readonly string _containerName = "test-files";
            private readonly string _fileName = "file.bin";
            private readonly DiskFileManager _diskFileManager;

            public GetFilePath()
            {
                _diskFileManager = new DiskFileManager(_rootFolder);
            }

            [Fact]
            public void BuildCorrectPath()
            {
                var filePath = _diskFileManager.GetFilePath(_fileId, _containerName, _fileName);
                var expected = @"Y:\Files\test-files\c3\b8\c3b836ef-ec43-4ac2-bba1-f477db3f480d_file.bin";
                Assert.Equal(expected, filePath);
            }

            [Theory]
            [InlineData("TestContainer", false)]
            [InlineData("TTT", false)]
            [InlineData("Test", false)]
            [InlineData("Test-Container", false)]
            [InlineData("test-container", true)]
            [InlineData("container", true)]
            [InlineData("---", true)]
            [InlineData("ttt", true)]
            [InlineData("container!", false)]
            [InlineData("info@dangl-it.com", false)]
            [InlineData("info@dangl", false)]
            [InlineData("test.container", false)]
            [InlineData("null", true)]
            [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)] // 63 chars - max length
            [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)] // 64 chars
            [InlineData("aaa", true)] // 3 chars - min length
            [InlineData("aa", false)] // 2 chars
            public void ArgumentExceptionForContainerWithUppercase(string containerName, bool isValid)
            {
                // This is to ensure compatibility with Azure containers, which only allows
                // lowercase alphanumeric chars and dash '-' container characters
                if (isValid)
                {
                    Assert.False(string.IsNullOrWhiteSpace(_diskFileManager.GetFilePath(_fileId, containerName, _fileName)));
                }
                else
                {
                    Assert.Throws<ArgumentException>("container", () => _diskFileManager.GetFilePath(_fileId, containerName, _fileName));
                }
            }

            [Fact]
            public void ArgumentNullExceptionForNullContainerName()
            {
                Assert.Throws<ArgumentNullException>("container", () => _diskFileManager.GetFilePath(_fileId, null, _fileName));
            }

            [Fact]
            public void ArgumentNullExceptionForEmptyContainerName()
            {
                Assert.Throws<ArgumentNullException>("container", () => _diskFileManager.GetFilePath(_fileId, string.Empty, _fileName));
            }

            [Fact]
            public void BuildCorrectPathWithNullFileName()
            {
                var filePath = _diskFileManager.GetFilePath(_fileId, _containerName, null);
                var expected = @"Y:\Files\test-files\c3\b8\c3b836ef-ec43-4ac2-bba1-f477db3f480d";
                Assert.Equal(expected, filePath);
            }

            [Fact]
            public void BuildCorrectPathWithEmptyFileName()
            {
                var filePath = _diskFileManager.GetFilePath(_fileId, _containerName, string.Empty);
                var expected = @"Y:\Files\test-files\c3\b8\c3b836ef-ec43-4ac2-bba1-f477db3f480d";
                Assert.Equal(expected, filePath);
            }

            [Fact]
            public void TruncatesFilenameIfTooLong()
            {
                // The file name part has a max length of 1024 chars to be compatible with Azure
                // storage limits
                var fileName = new string('a', 1024);
                var filePath = _diskFileManager.GetFilePath(_fileId, _containerName, fileName);
                var expectedBase = @"Y:\Files\test-files\c3\b8\";
                var guidPart = "c3b836ef-ec43-4ac2-bba1-f477db3f480d";
                var fileNamePart = "_" + new string('a', 1024 - guidPart.Length - 1); // -1 for the dash
                var expected = expectedBase + guidPart + fileNamePart;
                Assert.Equal(expected, filePath);
                Assert.True(filePath.Length <= 1024 + expectedBase.Length);
            }
        }
    }
}
