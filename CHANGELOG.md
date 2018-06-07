# Changelog

All notable changes to **Dangl.AspNetCore.FileHandling** are documented here.

## v0.1.3:
- Update `InMemoryFileManager` to create copies of the saved streams so they are accessible after the original was disposed. The `Clear()` method on the `InMemoryFileManager` now disposes all held streams before releasing them

## v0.1.2:
- Internal update of build system

## v0.1.1:
- Update package settings

## v0.1.0:
- Initial release
