# Utility Tests

This directory contains unit tests for the utility classes in the Adept.Core project.

## Purpose

The tests in this directory verify that utility classes correctly perform their functions and handle edge cases appropriately.

## Test Structure

Tests should be organized by utility class name, with each test file named after the class being tested, with the suffix "Tests".

For example:
- `StringUtilsTests.cs`
- `DateTimeUtilsTests.cs`
- `FileUtilsTests.cs`

## Test Guidelines

When writing utility tests, follow these guidelines:

1. Test each utility method with various inputs, including edge cases
2. Test both success and failure scenarios
3. For methods that throw exceptions, verify that the correct exceptions are thrown
4. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
5. Use the Arrange-Act-Assert pattern consistently
6. Consider using theory tests with inline data for testing multiple inputs

## Planned Tests

The following utility tests are planned for future implementation:

- StringUtilsTests.cs
- DateTimeUtilsTests.cs
- FileUtilsTests.cs
- JsonUtilsTests.cs
- ValidationUtilsTests.cs
