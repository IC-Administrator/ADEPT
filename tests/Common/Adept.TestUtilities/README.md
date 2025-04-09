# Adept.TestUtilities

This project provides shared test utilities for all ADEPT test projects.

## Overview

The `Adept.TestUtilities` project contains:

- **Test Fixtures**: Reusable test fixtures for database and file system tests
- **Test Helpers**: Common helper classes like `TestDataGenerator`, `MockFactory`, and `AssertExtensions`
- **Test Base Classes**: Base classes for different types of tests like `IntegrationTestBase` and `DatabaseTestBase`

## Components

### Test Fixtures

- **DatabaseFixture**: A fixture for database tests that provides a shared database context
- **FileSystemFixture**: A fixture for file system tests that provides a temporary directory

### Test Helpers

- **TestDataGenerator**: Helper class for generating test data
- **MockFactory**: Factory for creating common mock objects
- **AssertExtensions**: Extension methods for assertions

### Test Base Classes

- **IntegrationTestBase**: Base class for integration tests
- **DatabaseTestBase**: Base class for database tests

## Usage

To use the test utilities in your test project, add a reference to the `Adept.TestUtilities` project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Common\Adept.TestUtilities\Adept.TestUtilities.csproj" />
</ItemGroup>
```

Then, you can use the utilities in your tests:

```csharp
using Adept.TestUtilities.Helpers;
using Adept.TestUtilities.Fixtures;
using Adept.TestUtilities.TestBase;

// Use the test data generator
var messages = TestDataGenerator.GenerateRandomLlmMessages(3);

// Use the mock factory
var mockLogger = MockFactory.CreateMockLogger<MyService>();
var mockProvider = MockFactory.CreateMockLlmProvider();

// Use the assert extensions
AssertExtensions.FileExists(filePath);
AssertExtensions.StringContainsAll(value, "substring1", "substring2");

// Use the test base classes
public class MyIntegrationTest : IntegrationTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Configure services for the test
    }
}

// Use the test fixtures
public class MyFileSystemTest : IClassFixture<FileSystemFixture>
{
    private readonly FileSystemFixture _fixture;

    public MyFileSystemTest(FileSystemFixture fixture)
    {
        _fixture = fixture;
    }
}
```
