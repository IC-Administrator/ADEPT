# MCP Tool Provider Tests

This directory contains unit tests for the MCP (Multi-Capability Provider) tool providers in the Adept.Services project.

## Test Files

- **PuppeteerToolProviderTests.cs**: Tests for the `PuppeteerToolProvider` class and related functionality.

## Adding New Tests

When adding new MCP tool provider tests, follow these guidelines:

1. Name the test file after the tool provider being tested, with the suffix "Tests"
2. Group tests by method being tested
3. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
4. Use the Arrange-Act-Assert pattern consistently

## Planned Tests

The following MCP tool provider tests are planned for future implementation:

- FileSystemToolProviderTests.cs
- CalendarToolProviderTests.cs
- WebSearchToolProviderTests.cs
- ExcelToolProviderTests.cs
- FetchToolProviderTests.cs
- SequentialThinkingToolProviderTests.cs
