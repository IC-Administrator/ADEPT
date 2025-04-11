# ADEPT Test Documentation

This document provides an overview of the test organization and instructions for running tests in the ADEPT project.

## Test Organization

The ADEPT test suite is organized into the following structure:

```
tests/
├── Unit/                      # Unit tests
│   ├── Adept.Core.Tests/      # Core unit tests
│   ├── Adept.Services.Tests/  # Services unit tests
│   └── Adept.Data.Tests/      # Data unit tests
├── Integration/               # Integration tests
│   ├── Adept.FileSystem.Tests/ # File system integration tests
│   └── Adept.Database.Tests/  # Database integration tests
├── Manual/                    # Manual test applications
│   ├── Adept.FileSystem.ManualTests/ # File system manual tests
│   └── Adept.Llm.ManualTests/ # LLM manual tests
└── Common/                    # Shared test utilities
    └── Adept.TestUtilities/   # Common test helpers and fixtures
```

### Test Types

#### Unit Tests

Unit tests focus on testing individual components in isolation. They use mocks and stubs to replace dependencies and focus on the behavior of a single unit of code.

- **Adept.Core.Tests**: Tests for core models, interfaces, and utilities
- **Adept.Services.Tests**: Tests for service implementations
- **Adept.Data.Tests**: Tests for data access and validation

#### Integration Tests

Integration tests focus on testing the interaction between multiple components. They use real implementations of dependencies and focus on the behavior of the system as a whole.

- **Adept.FileSystem.Tests**: Tests for file system operations
- **Adept.Database.Tests**: Tests for database operations

#### Manual Tests

Manual tests are interactive applications that allow manual testing of specific components. They provide a user interface for testing functionality that is difficult to test automatically.

- **Adept.FileSystem.ManualTests**: Interactive tests for file system operations
- **Adept.Llm.ManualTests**: Interactive tests for LLM integration

### Test Utilities

The `Adept.TestUtilities` project provides shared utilities for all test projects:

- **Test Fixtures**: Reusable test fixtures for various test scenarios:
  - `DatabaseFixture`: For database tests
  - `FileSystemFixture`: For file system tests
  - `ServiceFixture`: For service tests
  - `RepositoryFixture`: For repository tests
  - `JsonFixture`: For JSON serialization tests

- **Test Helpers**: Common helper classes for test data and assertions:
  - `TestDataGenerator`: Generates test data for various models and scenarios
  - `MockFactory`: Creates mock objects for dependencies
  - `AssertExtensions`: Provides additional assertions for common test scenarios

- **Test Base Classes**: Base classes for different types of tests:
  - `IntegrationTestBase`: Base class for integration tests
  - `DatabaseTestBase`: Base class for database tests

## Running Tests

### Running Unit Tests

Unit tests can be run using the `dotnet test` command:

```bash
# Run all unit tests
dotnet test tests/Unit

# Run specific unit tests
dotnet test tests/Unit/Adept.Core.Tests
dotnet test tests/Unit/Adept.Services.Tests
dotnet test tests/Unit/Adept.Data.Tests
```

### Running Integration Tests

Integration tests can be run using the `dotnet test` command:

```bash
# Run all integration tests
dotnet test tests/Integration

# Run specific integration tests
dotnet test tests/Integration/Adept.FileSystem.Tests
dotnet test tests/Integration/Adept.Database.Tests
```

### Running Manual Tests

Manual tests can be run using the `dotnet run` command:

```bash
# Run file system manual tests
dotnet run --project tests/Manual/Adept.FileSystem.ManualTests

# Run LLM manual tests
dotnet run --project tests/Manual/Adept.Llm.ManualTests
```

## Writing Tests

### Test Naming Conventions

#### Test Class Naming

Test classes should be named according to the following pattern:

```
[ClassUnderTest]Tests
```

For example:
- `LessonTemplateRepositoryTests`
- `SystemPromptServiceTests`
- `JsonHelperTests`

#### Test Method Naming

Test methods should be named according to the following pattern:

```
[MethodUnderTest]_[Scenario]_[ExpectedResult]
```

For example:
- `GetTemplateByIdAsync_ValidId_ReturnsTemplate`
- `GetTemplateByIdAsync_InvalidId_ReturnsNull`
- `AddTemplateAsync_ValidTemplate_AddsTemplateAndReturnsIt`

### Writing Unit Tests

Unit tests should follow these guidelines:

1. Use the `Adept.TestUtilities` project for common test utilities
2. Use the `MockFactory` to create mock objects
3. Use the `TestDataGenerator` to generate test data
4. Use the `AssertExtensions` for custom assertions
5. Organize tests by feature and class
6. Use descriptive test names that follow the naming convention
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

### Writing Integration Tests

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

### Writing Manual Tests

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

## Test Coverage

The test suite aims to provide comprehensive coverage of the ADEPT codebase:

- **Core**: Models, interfaces, and utilities
- **Services**: LLM integration, file system, calendar, and other services
- **Data**: Database access, validation, and repositories
- **UI**: User interface components (future)

## Continuous Integration

The test suite is integrated with the CI/CD pipeline to ensure that all tests pass before code is merged:

1. Unit tests are run on every pull request
2. Integration tests are run on every pull request
3. Manual tests are run periodically by the development team

## Troubleshooting

If you encounter issues running tests, try the following:

1. Ensure that all dependencies are installed
2. Ensure that the database is properly configured
3. Ensure that the file system permissions are correct
4. Check the test logs for error messages
5. Try running individual tests to isolate the issue

## Contributing

When contributing to the ADEPT project, please follow these guidelines:

1. Write tests for all new features and bug fixes
2. Ensure that all tests pass before submitting a pull request
3. Follow the existing test organization and naming conventions
4. Use the `Adept.TestUtilities` project for common test utilities
5. Document any special test requirements or setup

## License

The ADEPT test suite is licensed under the same license as the ADEPT project.
