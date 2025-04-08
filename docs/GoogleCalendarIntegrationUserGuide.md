# Google Calendar Integration User Guide

## Introduction

The Google Calendar integration allows you to synchronize your lesson plans, tasks, and events with Google Calendar. This integration provides a seamless way to view and manage your schedule across different devices and platforms.

## Features

- **OAuth 2.0 Authentication**: Secure authentication with Google Calendar API
- **Two-way Synchronization**: Changes made in the application are reflected in Google Calendar and vice versa
- **Recurring Events**: Support for creating and managing recurring events
- **Multiple Calendars**: Support for multiple Google Calendars
- **Calendar Sharing**: Share calendars with other users
- **Color Coding**: Assign different colors to different types of events
- **Reminders**: Set email and popup reminders for events
- **Attendees**: Add attendees to events and track their responses
- **Attachments**: Attach files to events

## Setup Instructions

### Prerequisites

Before you can use the Google Calendar integration, you need to:

1. Have a Google account
2. Enable the Google Calendar API in the Google Cloud Console
3. Create OAuth 2.0 credentials (Client ID and Client Secret)

### Step 1: Create a Google Cloud Project

1. Go to the [Google Cloud Console](https://console.cloud.google.com)
2. Click on "Select a project" at the top of the page
3. Click on "NEW PROJECT" in the popup
4. Enter a name for your project (e.g., "ADEPT Calendar Integration")
5. Click "CREATE"
6. Wait for the project to be created and then select it

### Step 2: Enable the Google Calendar API

1. In the Google Cloud Console, navigate to "APIs & Services" > "Library"
2. Search for "Google Calendar API"
3. Click on "Google Calendar API" in the search results
4. Click "ENABLE"

### Step 3: Create OAuth 2.0 Credentials

1. In the Google Cloud Console, navigate to "APIs & Services" > "Credentials"
2. Click "CREATE CREDENTIALS" and select "OAuth client ID"
3. Select "Desktop app" as the application type
4. Enter a name for the OAuth client (e.g., "ADEPT Calendar Client")
5. Click "CREATE"
6. A popup will display your Client ID and Client Secret
7. Click "OK" to close the popup
8. Download the credentials by clicking the download icon (JSON) next to your new OAuth client ID

### Step 4: Configure the Application

1. Open the application
2. Navigate to Settings > Calendar
3. Enter the Client ID and Client Secret from the downloaded JSON file
4. Click "Save Credentials"
5. Click "Authenticate" to start the OAuth flow
6. A browser window will open asking you to sign in to your Google account
7. Sign in and grant the requested permissions
8. After successful authentication, the browser will redirect to a success page
9. Return to the application, which should now show "Authenticated" status

## Using the Google Calendar Integration

### Viewing Calendars

1. Navigate to the Calendar section of the application
2. All your Google Calendars will be listed in the sidebar
3. Click on a calendar to view its events
4. Use the checkboxes to show/hide specific calendars

### Creating Events

1. Click on the "+" button or a time slot in the calendar
2. Enter the event details:
   - Title
   - Description
   - Location
   - Start and end times
   - Calendar (if you have multiple calendars)
   - Color
   - Reminders
   - Attendees
   - Attachments
3. Click "Save" to create the event
4. The event will be synchronized with Google Calendar

### Creating Recurring Events

1. Follow the steps to create an event
2. In the event creation form, click on "Repeat"
3. Select a recurrence pattern:
   - Daily
   - Weekly
   - Monthly
   - Yearly
   - Custom
4. Set the recurrence options (e.g., repeat every 2 weeks on Monday and Wednesday)
5. Set an end date or number of occurrences
6. Click "Save" to create the recurring event

### Editing Events

1. Click on an event in the calendar
2. Click "Edit" in the event details popup
3. Make your changes to the event
4. For recurring events, choose whether to edit:
   - This occurrence only
   - This and all future occurrences
   - All occurrences
5. Click "Save" to update the event
6. The changes will be synchronized with Google Calendar

### Deleting Events

1. Click on an event in the calendar
2. Click "Delete" in the event details popup
3. For recurring events, choose whether to delete:
   - This occurrence only
   - This and all future occurrences
   - All occurrences
4. Confirm the deletion
5. The event will be removed from Google Calendar

### Sharing Calendars

1. Navigate to Settings > Calendar
2. Select the calendar you want to share
3. Click "Share Calendar"
4. Enter the email address of the person you want to share with
5. Set their permission level:
   - See only free/busy (hide details)
   - See all event details
   - Make changes to events
   - Make changes and manage sharing
6. Click "Add Person" to share the calendar
7. The person will receive an email notification about the shared calendar

### Managing Multiple Calendars

1. Navigate to Settings > Calendar
2. Click "Add Calendar" to add a new Google Calendar
3. Enter a name for the calendar
4. Select a color for the calendar
5. Click "Create" to create the new calendar
6. The new calendar will appear in the sidebar

### Setting Default Calendar

1. Navigate to Settings > Calendar
2. Select a calendar from the dropdown list
3. Click "Set as Default" to make it the default calendar for new events

## Integration with Other Features

### Lesson Planner Integration

The Google Calendar integration works seamlessly with the Lesson Planner:

1. When you create or update a lesson plan, it automatically creates or updates a corresponding event in Google Calendar
2. The event includes all relevant lesson details, including:
   - Lesson title
   - Class information
   - Lesson objectives
   - Location
   - Resources needed
3. Changes made to the lesson plan are automatically synchronized with the calendar event
4. If you enable two-way synchronization, changes made to the event in Google Calendar will be reflected in the lesson plan

### Task Manager Integration

The Google Calendar integration also works with the Task Manager:

1. Tasks with due dates can be displayed on your calendar
2. You can choose which task lists to display on the calendar
3. Completed tasks can be automatically hidden from the calendar
4. Task reminders can be synchronized with Google Calendar reminders

### Notification System Integration

The Google Calendar integration is connected to the application's notification system:

1. Calendar event reminders trigger notifications in the application
2. You can customize notification settings for different types of events
3. Notifications can be sent via:
   - In-app notifications
   - Email
   - Desktop notifications (if enabled)

## Troubleshooting

### Authentication Issues

If you encounter authentication issues:

1. Check that your Client ID and Client Secret are correct
2. Verify that the Google Calendar API is enabled in your Google Cloud project
3. Try re-authenticating by clicking "Re-authenticate" in Settings > Calendar
4. Check your internet connection
5. Ensure that your computer's date and time are correct

### Synchronization Issues

If events are not synchronizing properly:

1. Check your internet connection
2. Verify that you're authenticated with Google Calendar
3. Check the synchronization settings in Settings > Calendar
4. Try manually triggering a sync by clicking "Sync Now"
5. Check the application logs for detailed error messages

### Calendar Visibility Issues

If calendars or events are not visible:

1. Check that the calendar is selected in the sidebar
2. Verify that you have permission to view the calendar
3. Check the date range you're viewing
4. Try refreshing the calendar view
5. Check if the events are hidden by any active filters

## Advanced Features

### Calendar API Quotas

The Google Calendar API has usage limits:

1. The application is designed to work within these limits
2. If you have a large number of events, the application may throttle synchronization to avoid exceeding quotas
3. You can monitor your API usage in the Google Cloud Console

### Offline Mode

The application supports offline mode for calendar events:

1. Events are cached locally when you're online
2. You can view and edit events while offline
3. Changes made offline will be synchronized when you reconnect
4. Conflicts are resolved based on your conflict resolution settings

### Data Privacy

Your calendar data privacy is important:

1. The application only requests the minimum permissions needed
2. Your Google credentials are stored securely
3. No calendar data is sent to third parties
4. You can revoke access at any time through Google Account settings or the application

## Keyboard Shortcuts

For power users, the following keyboard shortcuts are available:

- `Ctrl+N`: Create a new event
- `Ctrl+E`: Edit the selected event
- `Delete`: Delete the selected event
- `Ctrl+F`: Search events
- `Ctrl+1` to `Ctrl+9`: Switch between different calendar views
- `Ctrl+S`: Manually trigger synchronization
- `Ctrl+R`: Refresh the calendar view

## Getting Help

If you need additional help with the Google Calendar integration:

1. Check the application's help documentation
2. Visit the support forum
3. Contact support via email
4. Check for updates to the application
