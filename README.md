# Dangl.AspNetCore.FileHandling

[![Build Status](https://jenkins.dangl.me/buildStatus/icon?job=GeorgDangl%2FDangl.AspNetCore.FileHandling%2Fdevelop)](https://jenkins.dangl.me/job/GeorgDangl/job/Dangl.AspNetCore.FileHandling/job/develop/)  
[![NuGet](https://img.shields.io/nuget/v/Dangl.AspNetCore.FileHandling.svg)](https://www.nuget.org/packages/Dangl.AspNetCore.FileHandling)  

[Link to documentation](https://docs.dangl-it.com/Projects/Dangl.AspNetCore.FileHandling)

[Changelog](./CHANGELOG.md)

The **Dangl.AspNetCore.FileHandling** package offers reusable tasks for projects that deal with file I/O, for example disk or Azure Blob storage access.

## Features

The `FileHandlerDefaults` class defines limits to adhere to when using file and container names. It is enforced to ensure a compatibility with Azure blob storage.

### IFileManager

The `IFileManager` interface defines how to store and retrieve files.

### DiskFileManager

The `DiskFileManager` works by storing files on a disk drive.

### InMemoryFileManager

For test purposes, the `InMemoryFileManager` offers additional features like `ClearFiles()` to reset all saved files and a property `SavedFiles` to access all saved files.

### InstanceInMemoryFileManager

For test purposes, the `InstanceInMemoryFileManager` offers additional features like `ClearFiles()` to reset all saved files and a property `SavedFiles` to access all saved files.

This implementation will keep its internal cache per instance, thus making it possible to run parallel tests that are independent of eachother.

### AzureBlobFileManager

This implementation works against Azure Blob Storage. Additionally, it has a `Task<RepositoryResult> EnsureContainerCreated(string container)` for initialization purposes.
Azure Blob containers must be created before they can be accessed.

#### SAS Uploads

To directly upload files to Azure Blob Storage, you can use the `AzureBlobFileManager` to generate SAS links:

Example:

```csharp
var fileManager = new AzureBlobFileManager(blobStorageConnectionString);
await fileManager.EnsureContainerCreated(containerName);

var sasLink = await fileManager.GetSasUploadLinkAsync(containerName, fileName);
if (sasLink.IsSuccess)
{
    var sasBlobClient = new BlobClient(new Uri(sasLink.Value.UploadLink));
    var uploadResponse = await sasBlobClient.UploadAsync(fileStream);
}
```

### StringExtensions

The `StringExtensions` class has a static extension method `string WithMaxLength(this string value, int maxLength)`.

## Extensions

This library offers extensions for dependency injection:
* `AddInMemoryFileManager()` for testing
* `AddDiskFileManager(string rootFolder)`
* `AddAzureBlobFileManager(string storageConnectionString)`

## Assembly Strong Naming & Usage in Signed Applications

This module produces strong named assemblies when compiled. When consumers of this package require strongly named assemblies, for example when they
themselves are signed, the outputs should work as-is.
The key file to create the strong name is adjacent to the `csproj` file in the root of the source project. Please note that this does not increase
security or provide tamper-proof binaries, as the key is available in the source code per 
[Microsoft guidelines](https://msdn.microsoft.com/en-us/library/wd40t7ad(v=vs.110).aspx)

---

[MIT Licence](LICENCE.md)
