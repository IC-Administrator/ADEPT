# Calendar Service Tests

This directory contains unit tests for the calendar services in the Adept.Services project.

## Purpose

The tests in this directory verify that calendar services correctly interact with calendar providers (such as Google Calendar) and handle calendar operations appropriately.

## Test Structure

Tests should be organized by service or component, with each test file named after the component being tested, with the suffix "Tests".

For example:
- `CalendarServiceTests.cs`
- `GoogleCalendarProviderTests.cs`
- `CalendarSyncServiceTests.cs`
- `RecurringEventServiceTests.cs`

## Test Guidelines

When writing calendar service tests, follow these guidelines:

1. Mock external API calls to avoid actual API usage during tests
2. Test both success and failure scenarios
3. Test authentication and authorization handling
4. Test event creation, reading, updating, and deletion
5. Test handling of recurring events
6. Test calendar synchronization functionality
7. Follow the naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
8. Use the Arrange-Act-Assert pattern consistently

## Planned Tests

The following calendar service tests are planned for future implementation:

- CalendarServiceTests.cs: Tests for the main calendar service
- GoogleCalendarProviderTests.cs: Tests for the Google Calendar provider
- CalendarSyncServiceTests.cs: Tests for calendar synchronization
- RecurringEventServiceTests.cs: Tests for handling recurring events
- CalendarNotificationServiceTests.cs: Tests for calendar notifications
- CalendarIntegrationServiceTests.cs: Tests for integration with other services
