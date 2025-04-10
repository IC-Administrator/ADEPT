# UI Manual Testing Guide

This guide provides step-by-step instructions for manually testing the UI enhancements in the ADEPT AI Teaching Assistant application.

## Prerequisites

- The application is built and running
- You have access to a keyboard and mouse
- You have a screen with adjustable resolution

## Test Scenarios

### 1. Visual Appearance Testing

#### 1.1 Style Consistency

1. Launch the application
2. Navigate through each tab (Home, Classes, Lesson Planner, Chat, Configuration)
3. Verify that:
   - All buttons of the same type (primary, secondary, icon) have consistent styling
   - Text elements (headers, body text, captions) have consistent styling
   - Cards and containers have consistent styling
   - Form controls (text boxes, combo boxes, check boxes) have consistent styling
   - Spacing and padding are consistent throughout the application

#### 1.2 Color Scheme

1. Verify that the application uses a consistent color scheme
2. Check that:
   - Primary actions are highlighted with the primary color
   - Secondary actions use the secondary color
   - Information is presented with appropriate contrast
   - Status indicators use appropriate colors (success, warning, error)

### 2. Animation and Transition Testing

#### 2.1 Tab Transitions

1. Navigate between tabs (Home, Classes, Lesson Planner, Chat, Configuration)
2. Verify that:
   - Transitions between tabs are smooth
   - Content fades in/out appropriately
   - No visual glitches occur during transitions

#### 2.2 Loading Animations

1. Perform operations that trigger loading states:
   - Refresh the class list
   - Generate a lesson plan
   - Send a chat message
2. Verify that:
   - Loading indicators appear and animate correctly
   - Loading indicators are properly positioned
   - Loading indicators disappear when the operation completes

#### 2.3 Button Animations

1. Hover over various buttons
2. Click on buttons
3. Verify that:
   - Buttons have hover effects
   - Buttons have click/press effects
   - Animations are smooth and consistent

#### 2.4 Notification Animations

1. Trigger notifications:
   - Save configuration settings (success)
   - Try an operation that would fail (error)
2. Verify that:
   - Notifications appear with a smooth animation
   - Notifications are properly positioned
   - Notifications disappear with a smooth animation

### 3. Responsive Design Testing

#### 3.1 Window Resizing

1. Launch the application at its default size
2. Resize the window to be smaller (approximately 800x600)
3. Resize the window to be very small (approximately 640x480)
4. Verify that:
   - UI elements adjust to the available space
   - Text remains readable
   - Important controls remain accessible
   - Scrollbars appear when needed

#### 3.2 Layout Adaptation

1. With a small window size, navigate through each tab
2. Verify that:
   - Layouts adapt appropriately
   - Panels collapse or reposition as needed
   - Controls remain usable
   - No content is cut off or inaccessible

### 4. Accessibility Testing

#### 4.1 Keyboard Navigation

1. Use the Tab key to navigate through the application
2. Verify that:
   - All interactive elements can be focused
   - Focus indicators are clearly visible
   - The tab order is logical
   - Actions can be triggered with the Enter key

#### 4.2 Screen Reader Compatibility

1. Enable a screen reader (e.g., Narrator on Windows)
2. Navigate through the application
3. Verify that:
   - Controls have proper automation names
   - Status changes are announced
   - Form fields have proper labels
   - Error messages are announced

#### 4.3 Visual Accessibility

1. Check text contrast throughout the application
2. Verify that:
   - Text has sufficient contrast against its background
   - Interactive elements are clearly distinguishable
   - Error states are clearly indicated
   - Focus indicators are clearly visible

### 5. Specific Feature Testing

#### 5.1 Classes Tab

1. Navigate to the Classes tab
2. Test the class list:
   - Verify that classes are displayed with proper styling
   - Select a class and verify that the selection is visually indicated
3. Test the student list:
   - Verify that students are displayed with proper styling
   - Select a student and verify that the selection is visually indicated
4. Test the toolbar buttons:
   - Verify that buttons have proper styling
   - Hover over buttons to check hover effects
   - Click buttons to check click effects

#### 5.2 Lesson Planner Tab

1. Navigate to the Lesson Planner tab
2. Test the date navigation:
   - Verify that the date controls have proper styling
   - Use the previous/next buttons to change the date
3. Test the lesson list:
   - Verify that lessons are displayed with proper styling
   - Select a lesson and verify that the selection is visually indicated
4. Test the lesson details form:
   - Verify that form controls have proper styling
   - Enter data in the form fields
5. Test the toolbar buttons:
   - Verify that buttons have proper styling
   - Hover over buttons to check hover effects
   - Click buttons to check click effects

#### 5.3 Chat Tab

1. Navigate to the Chat tab
2. Test the message list:
   - Verify that messages are displayed with proper styling
   - Check that user and assistant messages are visually distinct
3. Test the input area:
   - Verify that the input field has proper styling
   - Enter a message in the input field
4. Test the send button:
   - Verify that the button has proper styling
   - Hover over the button to check hover effects
   - Click the button to check click effects

#### 5.4 Configuration Tab

1. Navigate to the Configuration tab
2. Test the tab navigation:
   - Verify that tab buttons have proper styling
   - Click on different tabs to navigate
3. Test the settings forms:
   - Verify that form controls have proper styling
   - Enter data in the form fields
4. Test the save buttons:
   - Verify that buttons have proper styling
   - Hover over buttons to check hover effects
   - Click buttons to check click effects

## Recording Results

Use the UI_Testing_Checklist.md file to record the results of your testing. For each item:
- Check the box if the feature works correctly
- Leave the box unchecked if there are issues
- Add notes about any issues or inconsistencies found

## Reporting Issues

For any issues found during testing, record the following information:
1. The specific feature or component with the issue
2. Steps to reproduce the issue
3. Expected behavior
4. Actual behavior
5. Screenshots if applicable
