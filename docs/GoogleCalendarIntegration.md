# Google Calendar Integration

## Overview
The Google Calendar integration allows the application to synchronize lesson plans with Google Calendar events. This enables users to view their lesson plans in their Google Calendar and keep them in sync with the application.

## Features
- OAuth 2.0 authentication with Google Calendar API
- Two-way synchronization between lesson plans and calendar events
- Create, update, and delete calendar events
- View calendar events in the application
- Automatic token refresh

## Setup Instructions

### Prerequisites
- Google Cloud Console account
- Google Calendar API enabled
- OAuth 2.0 credentials (Client ID and Client Secret)

### Setting Up Google Cloud Console
1. Go to the [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project or select an existing one
3. Navigate to "APIs & Services" > "Library"
4. Search for "Google Calendar API" and enable it
5. Navigate to "APIs & Services" > "Credentials"
6. Click "Create Credentials" > "OAuth client ID"
7. Select "Web application" as the application type
8. Add "http://localhost:8080" as an authorized redirect URI
9. Click "Create" to generate the Client ID and Client Secret
10. Copy the Client ID and Client Secret for use in the application

### Configuring the Application
1. Open the application
2. Navigate to Settings > Calendar
3. Enter the Client ID and Client Secret
4. Click "Save Credentials"
5. Click "Authenticate" to start the OAuth flow
6. Follow the prompts to authorize the application
7. Once authenticated, the status will show "Authenticated"

## Usage

### Synchronizing Lesson Plans
1. Create or update a lesson plan in the application
2. The lesson plan will automatically be synchronized with Google Calendar
3. Alternatively, click "Synchronize All Lessons" in the Calendar Settings to manually synchronize all lesson plans

### Viewing Calendar Events
1. Open Google Calendar
2. Events created by the application will be visible in your calendar
3. Events will include the lesson plan title, description, and time

### Updating Calendar Events
1. Update a lesson plan in the application
2. The corresponding calendar event will be updated automatically
3. Changes made in Google Calendar will not be synchronized back to the application (one-way sync)

### Deleting Calendar Events
1. Delete a lesson plan in the application
2. The corresponding calendar event will be deleted automatically
3. Alternatively, delete the event directly in Google Calendar

## Architecture

### Components
- **GoogleCalendarService**: Handles communication with the Google Calendar API
- **GoogleOAuthService**: Manages OAuth authentication and token refresh
- **CalendarSyncService**: Synchronizes lesson plans with calendar events
- **CalendarSettingsViewModel**: Provides UI for configuring calendar settings

### Authentication Flow
1. User enters OAuth credentials and clicks "Authenticate"
2. Application opens a browser window for Google authentication
3. User logs in and grants permissions
4. Google redirects to the callback URL with an authorization code
5. Application exchanges the code for access and refresh tokens
6. Tokens are securely stored for future use
7. Access token is automatically refreshed when expired

### Data Flow
1. Lesson plan is created or updated in the application
2. CalendarSyncService formats the lesson plan data for Google Calendar
3. GoogleCalendarService sends the data to the Google Calendar API
4. Google Calendar API creates or updates the event
5. Event ID is stored with the lesson plan for future updates

## Troubleshooting

### Authentication Issues
- Ensure Client ID and Client Secret are correct
- Verify that the Google Calendar API is enabled
- Check that the redirect URI is correctly configured
- Try revoking authentication and authenticating again

### Synchronization Issues
- Check that the application is authenticated with Google Calendar
- Verify that lesson plans have valid dates and times
- Check the application logs for detailed error messages
- Try manually synchronizing all lessons

### API Limits
- Google Calendar API has usage limits
- If you encounter rate limit errors, reduce the frequency of synchronization
- Consider implementing batch operations for multiple events

## Security Considerations
- OAuth credentials are stored securely using the application's secure storage
- Access tokens are automatically refreshed and not exposed to the user
- The application requests only the necessary permissions for calendar operations
- Users can revoke access at any time through Google Account settings or the application
