# Dangl.AspNetCore.FileHandling

[![Build Status](https://jenkins.dangl.me/buildStatus/icon?job=Dangl.AspNetCore.FileHandling/develop)](https://jenkins.dangl.me/job/Dangl.AspNetCore.FileHandling/)  
[![NuGet](https://img.shields.io/nuget/v/Dangl.AspNetCore.FileHandling.svg)](https://www.nuget.org/packages/Dangl.AspNetCore.FileHandling)  
[![Built with Nuke](http://nuke.build/rounded)](https://www.nuke.build)

[Link to documentation](https://docs.dangl-it.com/Projects/Dangl.AspNetCore.FileHandling)

[Changelog](./CHANGELOG.md)

The **Dangl.AspNetCore.FileHandling** package offers reusable tasks for projects that deal with file I/O, for example disk or Azure Blob storage access.

## Features

The `FileHandlerDefaults` class defines limits to adhere to when using file and container names. It is enforced to ensure a compatibility with Azure blob storage.

### IFileHandler

The `IFileHandler` interface defines how to store and retrieve files.

### DiskFileHandler

The `DiskFileHandler` works by storing files on a disk drive.

### InMemoryFileHandler

For test purposes, the `InMemoryFileHandler` offers additional features like `ClearFiles()` to reset all saved files and a property `SavedFiles` to access all saved files.

### AzureBlobFileManager

This implementation works against Azure Blob Storage. Additionally, it has a `Task<RepositoryResult> EnsureContainerCreated(string container)` for initialization purposes.
Azure Blob containers must be created before they can be accessed.

### StringExtensions

The `StringExtensions` class has a static extension method `string WithMaxLength(this string value, int maxLength)`.

## Extensions

This library offers extensions for dependency injection:
* `AddInMemoryFileManager()` for testing
* `AddDiskFileManager(string rootFolder)`
* `AddAzureBlobFileManager(string storageConnectionString)`

---

[MIT Licence](LICENCE.md)
