# Model Tests

This directory contains unit tests for the models in the Adept.Core project.

## Purpose

The tests in this directory verify that models correctly implement their properties, methods, and validation rules.

## Test Structure

Tests should be organized by model, with each test file named after the model being tested, with the suffix "Tests".

For example:
- `LlmMessageTests.cs`
- `LessonResourceTests.cs`
- `LessonTemplateTests.cs`

## Test Guidelines

When writing model tests, follow these guidelines:

1. Test property getters and setters
2. Test default values and constructors
3. Test model validation rules
4. Test any methods implemented by the model
5. Test serialization and deserialization if applicable
6. Follow the naming convention: `[PropertyOrMethod]_[Scenario]_[ExpectedResult]`
7. Use the Arrange-Act-Assert pattern consistently

## Existing Tests

- `LlmMessageTests.cs`: Tests for the LlmMessage model

## Planned Tests

The following model tests are planned for future implementation:

- LessonResourceTests.cs
- LessonTemplateTests.cs
- UserTests.cs
- SettingsTests.cs
