# Manual Tests

This directory contains manual test applications for the ADEPT project.

## Overview

Manual tests are interactive applications that allow manual testing of specific components. They provide a user interface for testing functionality that is difficult to test automatically.

## Projects

- **Adept.FileSystem.ManualTests**: Interactive tests for file system operations
- **Adept.Llm.ManualTests**: Interactive tests for LLM integration

## Running Tests

Manual tests can be run using the `dotnet run` command:

```bash
# Run file system manual tests
dotnet run --project tests/Manual/Adept.FileSystem.ManualTests

# Run LLM manual tests
dotnet run --project tests/Manual/Adept.Llm.ManualTests
```

## Writing Manual Tests

Manual tests should follow these guidelines:

1. Use a menu-based interface for selecting tests
2. Provide clear instructions for each test
3. Display the results of each test
4. Handle errors gracefully
5. Clean up resources after tests

Example:

```csharp
private static async Task TestBasicFileOperationsAsync()
{
    Console.WriteLine("Testing Basic File Operations");
    Console.WriteLine("-----------------------------");

    // Ensure standard folders exist
    await _fileSystemService.EnsureStandardFoldersExistAsync();
    Console.WriteLine("Standard folders created");

    // Create a test file
    var testFilePath = "test.txt";
    var testContent = "This is a test file created by the manual test program.";
    await _fileSystemService.WriteFileAsync(testFilePath, testContent);
    Console.WriteLine($"Test file created: {testFilePath}");

    // Read the test file
    var content = await _fileSystemService.ReadFileAsync(testFilePath);
    Console.WriteLine($"File content: {content}");
}
```
