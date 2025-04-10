# Calendar Integration Tests

This directory contains integration tests for calendar services that interact with external calendar providers such as Google Calendar.

## Purpose

These tests verify that:
- Authentication with calendar providers works correctly
- Calendar operations (list, create, update, delete) function as expected
- Calendar synchronization works properly
- Error handling is appropriate

## Required Setup

To run these tests, you need:

1. **Google OAuth Credentials**:
   - Create a project in the [Google Cloud Console](https://console.cloud.google.com/)
   - Enable the Google Calendar API
   - Create OAuth 2.0 credentials (Web application type)
   - Set the redirect URI to `http://localhost:8080`
   - Store credentials in `appsettings.json` or use environment variables

2. **Test Configuration**:
   - Copy `appsettings.example.json` to `appsettings.json`
   - Fill in your OAuth credentials

## Test Categories

The tests are organized into the following categories:

- **Authentication**: Tests for OAuth flow and token management
- **CalendarOperations**: Tests for basic calendar operations
- **EventOperations**: Tests for event creation, reading, updating, and deletion
- **Synchronization**: Tests for two-way synchronization between local and remote calendars

## Running Tests

To run all calendar integration tests:

```bash
dotnet test tests/Integration/Adept.Services.Integration.Tests --filter "Category=Calendar"
```

To run a specific category of tests:

```bash
dotnet test tests/Integration/Adept.Services.Integration.Tests --filter "Category=Calendar&TestCategory=Authentication"
```

## Notes

- These tests interact with real Google Calendar API and may create/modify calendar data
- Tests are designed to clean up after themselves, but unexpected failures may leave test data
- Rate limits may apply when running tests repeatedly
- Consider using a dedicated test Google account rather than your primary account
