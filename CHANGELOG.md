# Changelog

All notable changes to **Dangl.AspNetCore.FileHandling** are documented here.

## v0.8.2:
- Enabled `ConfigureAwait(false)` for all async tasks

## v0.8.1:
- Added an overload to the constructor of `AzureBlobFileManager` back for binary compatibility with earlier versions

## v0.8.0:
- Dependency injection for the Azure Blob client was reworked, so it now also adds the original `BlobServiceClient` to the service collection
- The `AzureBlobFileManager` now implements `IAzureBlobFileManager` and offers additional, Azure Blob specific features. It is also registered as a service for type `IAzureBlobFileManager`
- `EnsureContainerCreated` was renamed to `EnsureContainerCreatedAsync` on the `AzureBlobFileManager` and is now also available on the `IAzureBlobFileManager` interface

## v0.7.1:
- Update Azure Storage dependencies

## 0.7.0:
- Added new `friendlyFileName` parameter when creating SAS links, so it's possible to manually configure the file name for downloading

## v0.6.2:
- Fixed a bug in the `InstanceInMemoryFileManager` and `InMemoryFileManager` classes, where getting a file stream, then disposing it, led to subsequent accesses to the file to fail

## v0.6.1:
- Added `AzureBlobFilePathBuilder` to provide a method to build file paths for Azure Blob Storage
- Added `GetSasDownloadLinkAsync` to `AzureBlobFileManager` to allow creation of direct SAS download links

## v0.6.0:
- Added `CheckIfFileExistsAsync` to the `IFileManager` interface
- The Azure Blob SAS link generation does now also work with non-Azure hosted endpoints and is now covered by an integration test

## v0.5.0:
- Switch to `Azure.Storage.Blobs` to replace the deprecated Azure SDK
- Add `GetSasUploadLinkAsync` method to `AzureBlobFileManager` to allow direct upload to blob storage for clients so the service does not have to proxy the file upload

## v0.4.0:
- Add the `InstanceInMemoryFileManager` class. This is different to the `InMemoryFileManager` in that it keeps its internal cache only per instance and not in a static field

## v0.3.0:
- Replace deprecated `WindowsAzure.Storage` dependency with `Microsoft.Azure.Storage.Blob` package

## v0.2.0:
- The generated assemblies now have a strong name. This is a breaking change of the binary API and will require recompilation on all systems that consume this package. The strong name of the generated assembly allows compatibility with other, signed tools. Please note that this does not increase security or provide tamper-proof binaries, as the key is available in the source code per [Microsoft guidelines](https://msdn.microsoft.com/en-us/library/wd40t7ad(v=vs.110).aspx)

## v0.1.6:
- Add option to get and save files with just their filename / filepath and a container name
- Add option to delete files

## v0.1.5:
- Add option to save timestamped files in a hierarchical structure

## v0.1.4:
- Add package `Dangl.AspNetCore.FileHandling.Azure`

## v0.1.3:
- Update `InMemoryFileManager` to create copies of the saved streams so they are accessible after the original was disposed. The `Clear()` method on the `InMemoryFileManager` now disposes all held streams before releasing them

## v0.1.2:
- Internal update of build system

## v0.1.1:
- Update package settings

## v0.1.0:
- Initial release
