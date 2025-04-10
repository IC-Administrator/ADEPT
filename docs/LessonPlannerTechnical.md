# Lesson Planner Technical Documentation

## Architecture Overview

The Lesson Planner module follows the MVVM (Model-View-ViewModel) architecture pattern:

- **Models**: Data structures in the `Adept.Core.Models` namespace
- **Views**: XAML-based UI components in the `Adept.UI.Views` namespace
- **ViewModels**: Business logic in the `Adept.UI.ViewModels` namespace
- **Controls**: Reusable UI components in the `Adept.UI.Controls` namespace
- **Converters**: Value converters in the `Adept.UI.Converters` namespace

## Key Components

### Models

#### LessonPlan

The core model representing a lesson:

```csharp
public class LessonPlan
{
    public string LessonId { get; set; }
    public string ClassId { get; set; }
    public string Title { get; set; }
    public DateTime Date { get; set; }
    public int TimeSlot { get; set; }
    public string LearningObjectives { get; set; }
    public string ComponentsJson { get; set; }
    public string CalendarEventId { get; set; }
    // Additional properties...
}
```

#### LessonResource

Represents a resource attached to a lesson:

```csharp
public class LessonResource
{
    public Guid ResourceId { get; set; }
    public Guid LessonId { get; set; }
    public string Name { get; set; }
    public ResourceType Type { get; set; }
    public string Path { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ResourceType
{
    File,
    Link,
    Image,
    Document,
    Presentation,
    Spreadsheet,
    Video,
    Audio,
    Other
}
```

#### LessonTemplate

Represents a reusable lesson template:

```csharp
public class LessonTemplate
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public string Title { get; set; }
    public string LearningObjectives { get; set; }
    public string ComponentsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### ViewModels

#### LessonPlannerViewModel

The main ViewModel that manages lesson planning functionality:

- Manages lesson creation, editing, and deletion
- Handles resource management
- Manages template operations
- Coordinates calendar view and navigation
- Handles Google Calendar synchronization

Key methods:
- `LoadLessonsForSelectedClassAsync()`: Loads lessons for the selected class
- `SaveLessonAsync()`: Saves the current lesson
- `DeleteLessonAsync()`: Deletes the selected lesson
- `LoadResourcesAsync()`: Loads resources for the selected lesson
- `AddFileResource()`: Adds a file resource to the lesson
- `AddLinkResource()`: Adds a link resource to the lesson
- `SaveAsTemplateAsync()`: Saves the current lesson as a template
- `ApplyTemplateAsync()`: Applies a template to the current lesson

### Controls

#### ResourceAttachmentControl

A user control for managing lesson resources:

- Displays a list of attached resources
- Provides UI for adding file and link resources
- Supports resource preview and removal

#### TemplateManagementControl

A user control for managing lesson templates:

- Displays a list of available templates
- Provides UI for saving lessons as templates
- Supports template search and filtering
- Allows applying templates to lessons

#### WeekCalendarControl

A user control for displaying and managing lessons in a week view:

- Shows lessons organized by day and time slot
- Supports drag-and-drop for rescheduling lessons
- Provides navigation between weeks

#### LessonComponentEditorControl

A user control for editing lesson components:

- Provides a tabbed interface for different component types
- Supports rich text editing
- Includes AI-powered suggestion generation

### Converters

#### ResourceTypeToIconConverter

Converts a `ResourceType` enum value to an icon image:

```csharp
public class ResourceTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ResourceType resourceType)
        {
            string iconPath;
            
            switch (resourceType)
            {
                case ResourceType.File:
                    iconPath = "/Adept.UI;component/Resources/Icons/file.png";
                    break;
                // Other cases...
                default:
                    iconPath = "/Adept.UI;component/Resources/Icons/other.png";
                    break;
            }
            
            return new BitmapImage(new Uri(iconPath, UriKind.Relative));
        }
        
        return null;
    }
}
```

#### StringToSyncStatusConverter

Converts a calendar event ID string to a sync status color:

```csharp
public class StringToSyncStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string calendarEventId = value as string;
        
        if (string.IsNullOrEmpty(calendarEventId))
            return new SolidColorBrush(Colors.Transparent);
        
        if (calendarEventId == "pending")
            return new SolidColorBrush(Colors.Yellow);
        
        if (calendarEventId == "error")
            return new SolidColorBrush(Colors.Red);
        
        return new SolidColorBrush(Colors.Green);
    }
}
```

## Database Schema

### LessonPlans Table

```sql
CREATE TABLE LessonPlans (
    LessonId TEXT PRIMARY KEY,
    ClassId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Date TEXT NOT NULL,
    TimeSlot INTEGER NOT NULL,
    LearningObjectives TEXT,
    ComponentsJson TEXT,
    CalendarEventId TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

### LessonResources Table

```sql
CREATE TABLE LessonResources (
    ResourceId TEXT PRIMARY KEY,
    LessonId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Type INTEGER NOT NULL,
    Path TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (LessonId) REFERENCES LessonPlans (LessonId) ON DELETE CASCADE
);
```

### LessonTemplates Table

```sql
CREATE TABLE LessonTemplates (
    TemplateId TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    Category TEXT,
    Tags TEXT,
    Title TEXT,
    LearningObjectives TEXT,
    ComponentsJson TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);
```

## Implementation Details

### Resource Management

The resource management system handles various types of resources:

1. **File Resources**: Local files stored on the user's computer
2. **Link Resources**: URLs to external websites or resources

The `ResourceAttachmentControl` provides the UI for managing these resources, while the `LessonPlannerViewModel` handles the business logic.

### Template Management

The template system allows users to save and reuse lesson structures:

1. Templates are stored in the database
2. Templates can be categorized and tagged for easy retrieval
3. Templates can be applied to new or existing lessons

The `TemplateManagementControl` provides the UI for managing templates, while the `LessonPlannerViewModel` handles the business logic.

### Calendar Integration

The Google Calendar integration allows users to sync their lessons with their Google Calendar:

1. Lessons can be added to Google Calendar
2. Changes to lessons can be synced to Google Calendar
3. The sync status is displayed in the UI

## Extension Points

### Adding New Resource Types

To add a new resource type:

1. Add a new value to the `ResourceType` enum
2. Update the `ResourceTypeToIconConverter` to handle the new type
3. Update the `GetResourceTypeFromExtension` method to recognize the new type
4. Update the `CreatePreviewContent` method to provide a preview for the new type

### Adding New Template Features

To enhance the template system:

1. Extend the `LessonTemplate` model with new properties
2. Update the `TemplateManagementControl` to display and edit the new properties
3. Modify the `SaveAsTemplateAsync` and `ApplyTemplateAsync` methods to handle the new properties

## Testing

Unit tests for the Lesson Planner module are located in the `Adept.Tests` project:

- `ResourceManagementTests.cs`: Tests for the resource management functionality
- `TemplateManagementTests.cs`: Tests for the template management functionality

To run the tests, use the `run-tests.bat` script or run the following command:

```
dotnet test src\Adept.Tests\Adept.Tests.csproj
```
