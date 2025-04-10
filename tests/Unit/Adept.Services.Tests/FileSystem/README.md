# File System Service Tests

This directory contains unit tests for the file system services in the Adept.Services project.

## Purpose

The tests in this directory verify that file system services correctly handle file operations, scratchpad management, and markdown processing.

## Test Structure

Tests should be organized by service or component, with each test file named after the component being tested, with the suffix "Tests".

For example:
- `FileSystemServiceTests.cs`
- `ScratchpadManagerTests.cs`
- `MarkdownProcessorTests.cs`
- `FileOrganizationServiceTests.cs`

## Test Guidelines

When writing file system service tests, follow these guidelines:

1. Use a temporary test directory for file operations to avoid affecting real files
2. Clean up test files and directories after tests complete
3. Test both success and failure scenarios
4. Test handling of various file types and formats
5. Test error handling for file operations
6. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
7. Use the Arrange-Act-Assert pattern consistently

## Planned Tests

The following file system service tests are planned for future implementation:

- FileSystemServiceTests.cs: Tests for basic file system operations
- ScratchpadManagerTests.cs: Tests for scratchpad creation, reading, and management
- MarkdownProcessorTests.cs: Tests for markdown parsing and processing
- FileOrganizationServiceTests.cs: Tests for file organization structure
