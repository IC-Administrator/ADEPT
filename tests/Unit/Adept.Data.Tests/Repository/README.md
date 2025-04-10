# Repository Tests

This directory contains unit tests for the repository classes in the Adept.Data project.

## Test Files

- **ResourceManagementTests.cs**: Tests for the `LessonResourceRepository` class and related functionality.
- **TemplateManagementTests.cs**: Tests for the `LessonTemplateRepository` class and related functionality.

## Adding New Tests

When adding new repository tests, follow these guidelines:

1. Name the test file after the repository being tested, with the suffix "Tests"
2. Group tests by method being tested
3. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
4. Use the Arrange-Act-Assert pattern consistently

## Test Data

Consider using the test data generators in the `TestData` folder for creating test entities.
