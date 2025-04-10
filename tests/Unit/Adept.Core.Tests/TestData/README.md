# Test Data

This directory contains test data for the Adept.Core.Tests project.

## Purpose

The files in this directory provide test data that can be used across multiple test classes, ensuring consistency and reducing duplication.

## Test Data Structure

Test data should be organized by the model or component it relates to, with clear naming conventions.

For example:
- `LessonResourceTestData.cs`
- `LessonTemplateTestData.cs`
- `LlmMessageTestData.cs`

## Guidelines for Test Data

When creating test data, follow these guidelines:

1. Create factory methods that return instances of models with predefined values
2. Provide methods for creating both valid and invalid instances for testing validation
3. Use descriptive names for test data factory methods
4. Consider using builder patterns for complex test data
5. Keep test data realistic but simple
6. Document any special considerations for the test data

## Planned Test Data

The following test data classes are planned for future implementation:

- LessonResourceTestData.cs
- LessonTemplateTestData.cs
- LlmMessageTestData.cs
- UserTestData.cs
- SettingsTestData.cs
