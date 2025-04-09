# Unit Tests

This directory contains unit tests for the ADEPT project.

## Overview

Unit tests focus on testing individual components in isolation. They use mocks and stubs to replace dependencies and focus on the behavior of a single unit of code.

## Projects

- **Adept.Core.Tests**: Tests for core models, interfaces, and utilities
- **Adept.Services.Tests**: Tests for service implementations
- **Adept.Data.Tests**: Tests for data access and validation

## Running Tests

Unit tests can be run using the `dotnet test` command:

```bash
# Run all unit tests
dotnet test tests/Unit

# Run specific unit tests
dotnet test tests/Unit/Adept.Core.Tests
dotnet test tests/Unit/Adept.Services.Tests
dotnet test tests/Unit/Adept.Data.Tests
```

## Writing Unit Tests

Unit tests should follow these guidelines:

1. Use the `Adept.TestUtilities` project for common test utilities
2. Use the `MockFactory` to create mock objects
3. Use the `TestDataGenerator` to generate test data
4. Use the `AssertExtensions` for custom assertions
5. Organize tests by feature and class
6. Use descriptive test names that describe the behavior being tested
7. Follow the Arrange-Act-Assert pattern

Example:

```csharp
[Fact]
public void ValidateClass_ValidClass_ReturnsValidResult()
{
    // Arrange
    var classEntity = new Class
    {
        ClassId = TestConstants.EntityIds.ClassId,
        ClassCode = TestConstants.EntityData.ClassCode,
        EducationLevel = "Undergraduate",
        CurrentTopic = "Introduction to Programming"
    };

    // Act
    var result = EntityValidator.ValidateClass(classEntity);

    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
}
```
