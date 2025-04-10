# UI Unit Tests

This directory contains unit tests for the Adept.UI project.

## Purpose

These tests verify the functionality of UI components and view models in isolation. They focus on:

- UI component behavior
- View model logic
- Data binding
- UI state management
- UI enhancements

## Test Categories

- **Component Tests**: Tests for individual UI components
- **ViewModel Tests**: Tests for view model logic
- **Enhancement Tests**: Tests for UI enhancements and improvements

## Running Tests

To run all UI unit tests:

```bash
dotnet test tests/Unit/Adept.UI.Tests
```

To run a specific test:

```bash
dotnet test tests/Unit/Adept.UI.Tests --filter "FullyQualifiedName~UIEnhancementTests"
```

## Relationship to Manual UI Testing

For manual UI testing documentation, see the `tests/UI` directory, which contains:
- UI testing instructions
- Testing checklists
- Feedback forms
- Testing scripts
