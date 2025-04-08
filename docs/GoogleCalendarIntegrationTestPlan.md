# Google Calendar Integration Test Plan

## Prerequisites
- Google Cloud Console account
- Google Calendar API enabled
- OAuth 2.0 credentials (Client ID and Client Secret)
- Authorized redirect URI set to http://localhost:8080

## Test Environment Setup
1. Configure OAuth credentials in the application settings
   - Open the application
   - Navigate to Calendar Settings
   - Enter the Client ID and Client Secret
   - Click "Save Credentials"

## Test Cases

### 1. Authentication Tests
| Test ID | Test Description | Expected Result |
|---------|-----------------|-----------------|
| AUTH-01 | Test OAuth authentication flow | Authentication successful, status shows "Authenticated" |
| AUTH-02 | Test token refresh | Token refreshes automatically when expired |
| AUTH-03 | Test token revocation | Authentication revoked, status shows "Not Authenticated" |
| AUTH-04 | Test authentication with invalid credentials | Error message displayed, authentication fails |

### 2. Calendar Event Retrieval Tests
| Test ID | Test Description | Expected Result |
|---------|-----------------|-----------------|
| GET-01 | Get events for today | Events for today are displayed correctly |
| GET-02 | Get events for a specific date | Events for the specified date are displayed correctly |
| GET-03 | Get events for a date range | Events for the date range are displayed correctly |
| GET-04 | Get events with no results | Empty list is returned, no errors |

### 3. Calendar Event Creation Tests
| Test ID | Test Description | Expected Result |
|---------|-----------------|-----------------|
| CREATE-01 | Create a new event | Event created successfully, event ID returned |
| CREATE-02 | Create an event with all fields | Event created with all fields populated correctly |
| CREATE-03 | Create an event with minimal fields | Event created with only required fields |
| CREATE-04 | Create an event with invalid data | Error message displayed, event not created |

### 4. Calendar Event Update Tests
| Test ID | Test Description | Expected Result |
|---------|-----------------|-----------------|
| UPDATE-01 | Update an existing event | Event updated successfully |
| UPDATE-02 | Update event summary | Event summary updated correctly |
| UPDATE-03 | Update event time | Event time updated correctly |
| UPDATE-04 | Update event with invalid data | Error message displayed, event not updated |

### 5. Calendar Event Deletion Tests
| Test ID | Test Description | Expected Result |
|---------|-----------------|-----------------|
| DELETE-01 | Delete an existing event | Event deleted successfully |
| DELETE-02 | Delete a non-existent event | Error message displayed, operation fails gracefully |

### 6. Synchronization Tests
| Test ID | Test Description | Expected Result |
|---------|-----------------|-----------------|
| SYNC-01 | Synchronize all lesson plans | All lesson plans synchronized with calendar |
| SYNC-02 | Synchronize a specific lesson plan | Lesson plan synchronized with calendar |
| SYNC-03 | Update a synchronized lesson plan | Calendar event updated accordingly |
| SYNC-04 | Delete a synchronized lesson plan | Calendar event deleted accordingly |

## Test Execution

### Using the GoogleCalendarTest Project
1. Open the GoogleCalendarTest project
2. Configure OAuth credentials in appsettings.json
3. Run the project
4. Follow the authentication flow
5. Use the menu to test different calendar operations

### Using the CalendarIntegrationTest Project
1. Open the CalendarIntegrationTest project
2. Configure OAuth credentials in appsettings.json
3. Run the project
4. Follow the authentication flow
5. The test will automatically run through various calendar operations

## Test Reporting
For each test case, record:
- Test ID
- Test status (Pass/Fail)
- Actual result
- Any errors or issues encountered
- Screenshots (if applicable)

## Troubleshooting
- Check OAuth credentials are correct
- Ensure Google Calendar API is enabled
- Verify redirect URI is correctly configured
- Check network connectivity
- Review application logs for detailed error messages
