# Validation Tests

This directory contains unit tests for the validation functionality in the Adept.Data project.

## Purpose

The tests in this directory verify that data validation works correctly, ensuring that only valid data is stored in the database.

## Test Structure

Tests should be organized by validator, with each test file named after the validator being tested, with the suffix "Tests".

For example:
- `EntityValidatorTests.cs`
- `LessonResourceValidatorTests.cs`
- `LessonTemplateValidatorTests.cs`

## Test Guidelines

When writing validation tests, follow these guidelines:

1. Test validation with both valid and invalid data
2. Test each validation rule separately
3. Test that appropriate error messages are returned for invalid data
4. Test validation of complex objects with nested properties
5. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
6. Use the Arrange-Act-Assert pattern consistently
7. Consider using theory tests with inline data for testing multiple validation scenarios

## Existing Tests

- `EntityValidatorTests.cs`: Tests for the generic entity validator

## Planned Tests

The following validation tests are planned for future implementation:

- LessonResourceValidatorTests.cs
- LessonTemplateValidatorTests.cs
- UserValidatorTests.cs
- SettingsValidatorTests.cs
