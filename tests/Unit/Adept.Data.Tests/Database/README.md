# Database Tests

This directory contains unit tests for the database functionality in the Adept.Data project.

## Purpose

The tests in this directory verify that database operations, migrations, and integrity checks work correctly.

## Test Structure

Tests should be organized by component, with each test file named after the component being tested, with the suffix "Tests".

For example:
- `DatabaseProviderTests.cs`
- `DatabaseMigrationTests.cs`
- `DatabaseBackupServiceTests.cs`
- `DatabaseIntegrityServiceTests.cs`

## Test Guidelines

When writing database tests, follow these guidelines:

1. Use in-memory SQLite databases for testing to avoid affecting real data
2. Set up and tear down test databases for each test to ensure isolation
3. Test both success and failure scenarios
4. Test database migrations to ensure they apply correctly
5. Test database backup and restore functionality
6. Test database integrity checks
7. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
8. Use the Arrange-Act-Assert pattern consistently

## Existing Tests

- `DatabaseBackupServiceTests.cs`: Tests for the database backup and restore functionality
- `DatabaseIntegrityServiceTests.cs`: Tests for the database integrity checking functionality

## Planned Tests

The following database tests are planned for future implementation:

- DatabaseProviderTests.cs
- DatabaseMigrationTests.cs
- DatabaseConnectionTests.cs
- DatabaseTransactionTests.cs
