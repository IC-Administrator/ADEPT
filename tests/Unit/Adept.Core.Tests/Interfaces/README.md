# Interface Tests

This directory contains unit tests for the interfaces in the Adept.Core project.

## Purpose

The tests in this directory verify that implementations of core interfaces correctly adhere to the interface contracts and behave as expected.

## Test Structure

Tests should be organized by interface name, with each test file named after the interface being tested, with the suffix "Tests".

For example:
- `ILessonServiceTests.cs`
- `ITemplateServiceTests.cs`
- `IResourceServiceTests.cs`

## Test Guidelines

When writing interface tests, follow these guidelines:

1. Test that implementations correctly implement all interface methods
2. Use mocks for dependencies to isolate the implementation being tested
3. Test both success and failure scenarios
4. Verify that implementations handle edge cases correctly
5. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
6. Use the Arrange-Act-Assert pattern consistently

## Planned Tests

The following interface tests are planned for future implementation:

- ILessonServiceTests.cs
- ITemplateServiceTests.cs
- IResourceServiceTests.cs
- ILlmServiceTests.cs
- IFileSystemServiceTests.cs
