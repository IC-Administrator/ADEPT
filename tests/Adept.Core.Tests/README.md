# Core Unit Tests

This directory contains unit tests for the Adept.Core project.

## Purpose

These tests verify the functionality of core components, including:

- Configuration classes
- Core interfaces
- Utility classes
- Model classes

## Test Categories

- **Configuration Tests**: Tests for configuration classes
- **Interface Tests**: Tests for interface implementations
- **Utility Tests**: Tests for utility classes
- **Model Tests**: Tests for model classes

## Running Tests

To run all core unit tests:

```bash
dotnet test tests/Adept.Core.Tests
```

To run a specific test:

```bash
dotnet test tests/Adept.Core.Tests --filter "FullyQualifiedName~CoreConfigurationTests"
```
