# Integration Tests

This directory contains integration tests for the ADEPT project.

## Overview

Integration tests focus on testing the interaction between multiple components. They use real implementations of dependencies and focus on the behavior of the system as a whole.

## Projects

- **Adept.Services.Integration.Tests**: Tests for service integrations with external systems
  - Calendar: Tests for calendar service integrations (Google Calendar, etc.)
  - FileSystem: Tests for file system operations
  - Database: Tests for database interactions
  - Llm: Tests for LLM provider integrations
  - Mcp: Tests for MCP tool integrations
- **Adept.FileSystem.Tests**: Tests for file system operations
- **Adept.Database.Tests**: Tests for database operations

## Running Tests

Integration tests can be run using the `dotnet test` command:

```bash
# Run all integration tests
dotnet test tests/Integration

# Run specific integration tests
dotnet test tests/Integration/Adept.FileSystem.Tests
dotnet test tests/Integration/Adept.Database.Tests
```

## Writing Integration Tests

Integration tests should follow these guidelines:

1. Use the `Adept.TestUtilities` project for common test utilities
2. Use the appropriate test fixture for the component being tested
3. Use real implementations of dependencies
4. Clean up resources after tests
5. Use descriptive test names that describe the behavior being tested
6. Follow the Arrange-Act-Assert pattern

Example:

```csharp
[Fact]
public async Task ReadFileAsync_ExistingFile_ReturnsContent()
{
    // Arrange
    var filePath = "test.txt";

    // Act
    var content = await _fixture.FileSystemService.ReadFileAsync(filePath);

    // Assert
    Assert.Equal("This is a test text file.", content);
}
```
