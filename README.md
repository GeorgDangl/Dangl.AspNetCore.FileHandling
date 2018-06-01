# Dangl.AspNetCore.FileHandling

[![Build Status](https://jenkins.dangl.me/buildStatus/icon?job=Dangl.AspNetCore.FileHandling.Tests)](https://jenkins.dangl.me/job/Dangl.AspNetCore.FileHandling.Tests/)

[Link to documentation](https://docs.dangl-it.com/Projects/Dangl.AspNetCore.FileHandling)

[Changelog](./CHANGELOG.md)

The **Dangl.AspNetCore.FileHandling** package offers reusable tasks for web project that deal with file I/O, for example disk or Azure Blob storage access.

## Features

The `FileHandlerDefaults` defaults limits to adhere to when using file and container names. It is enforced to ensure a compatibility with Azure blob storage.

### IFileHandler

The `IFileHandler` interface defines how to store and retrieve files.

### DiskFileHandler

The `DiskFileHandler` works by storing files on a disk drive.

### InMemoryFileHandler

For test purposes, the `InMemoryFileHandler` offers additional features like `ClearFiles()` to reset all saved files and a property `SavedFiles` to access all saved files.

### StringExtensions

The `StringExtensions` class has a static extension method `string WithMaxLength(this string value, int maxLength)`.

## Extensions

This library offers two extensions for dependency injection, `AddInMemoryFileManager()` (for testing) and `AddDiskFileManager(string rootFolder)`.