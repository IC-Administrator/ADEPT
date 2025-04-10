# Test Data

This directory contains test data for the Adept.Services.Tests project.

## Purpose

The files in this directory provide test data that can be used across multiple test classes, ensuring consistency and reducing duplication.

## Test Data Structure

Test data should be organized by the service or component it relates to, with clear naming conventions.

For example:
- `LlmTestData.cs`
- `FileSystemTestData.cs`
- `CalendarTestData.cs`
- `McpTestData.cs`

## Guidelines for Test Data

When creating test data, follow these guidelines:

1. Create factory methods that return test data with predefined values
2. Provide methods for creating both valid and invalid test data
3. Use descriptive names for test data factory methods
4. Consider using builder patterns for complex test data
5. Keep test data realistic but simple
6. Document any special considerations for the test data

## Planned Test Data

The following test data classes are planned for future implementation:

- LlmTestData.cs: Test data for LLM services, including prompts and responses
- FileSystemTestData.cs: Test data for file system services, including file content and metadata
- CalendarTestData.cs: Test data for calendar services, including events and calendars
- McpTestData.cs: Test data for MCP tools, including tool parameters and responses
- PuppeteerTestData.cs: Test data for Puppeteer services, including web page content and selectors
