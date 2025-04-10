# LLM Service Tests

This directory contains unit tests for the LLM (Large Language Model) services in the Adept.Services project.

## Purpose

The tests in this directory verify that LLM services correctly interact with various LLM providers and handle responses appropriately.

## Test Structure

Tests should be organized by service or provider, with each test file named after the component being tested, with the suffix "Tests".

For example:
- `LlmServiceTests.cs`
- `OpenAiProviderTests.cs`
- `AnthropicProviderTests.cs`
- `GoogleProviderTests.cs`
- `MetaProviderTests.cs`
- `OpenRouterProviderTests.cs`
- `DeepSeekProviderTests.cs`
- `LlmServiceFallbackTests.cs`
- `TokenCounterTests.cs`

## Test Guidelines

When writing LLM service tests, follow these guidelines:

1. Mock external API calls to avoid actual API usage during tests
2. Test both success and failure scenarios
3. Test fallback mechanisms when primary providers fail
4. Test token counting and rate limiting functionality
5. Test prompt formatting and response parsing
6. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
7. Use the Arrange-Act-Assert pattern consistently

## Existing Tests

- `LlmServiceFallbackTests.cs`: Tests for the LLM service fallback mechanism
- `TokenCounterTests.cs`: Tests for the token counting functionality

## Planned Tests

The following LLM service tests are planned for future implementation:

- OpenAiProviderTests.cs
- AnthropicProviderTests.cs
- GoogleProviderTests.cs
- MetaProviderTests.cs
- OpenRouterProviderTests.cs
- DeepSeekProviderTests.cs
- LlmServiceTests.cs
