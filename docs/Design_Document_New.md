# Design Document for ADEPT AI Teaching Assistant

## 1. Introduction

### 1.1 Purpose

This Design Document provides a comprehensive technical blueprint for the ADEPT (AI-Driven Education Planning Tool) Teaching Assistant. It serves as a bridge between the requirements phase and implementation, offering detailed guidance for completing the development of the system based on the current state of the project.

### 1.2 Scope

This document covers:
- Current state assessment of the ADEPT project
- Detailed implementation plan for remaining components
- Technical specifications for each subsystem
- Integration strategies for external services
- Testing approach and quality assurance
- Deployment and maintenance considerations

### 1.3 Intended Audience

This document is intended for:
- Developers working on the ADEPT project
- Technical reviewers evaluating the implementation
- Project stakeholders tracking development progress

### 1.4 References

1. User Requirements Specification (URS.md)
2. System Requirements Specification (SysRS.md)
3. Original Design Document (Design_Document_Original.md) - Kept for historical reference
4. Test Organization Plan (TestOrganization.md)
5. UI Improvement Plan (UI_Improvement_Plan.md)

## 2. Current State Assessment

### 2.1 Project Structure Overview

The ADEPT project follows a modular architecture with clear separation of concerns:

```
ADEPT/
├── src/                      # Source code
│   ├── Adept.Common/         # Common interfaces and utilities
│   ├── Adept.Core/           # Core domain models and interfaces
│   ├── Adept.Data/           # Data access and repositories
│   ├── Adept.Services/       # Business logic and external integrations
│   └── Adept.UI/             # WPF user interface
├── tests/                    # Test projects
│   ├── Common/               # Test utilities
│   ├── Unit/                 # Unit tests
│   ├── Integration/          # Integration tests
│   ├── UI/                   # UI testing documentation
│   └── Manual/               # Manual test scripts
└── docs/                     # Documentation
```

### 2.2 Implementation Status

#### 2.2.1 Completed Components

1. **Project Structure and Architecture**
   - Basic MVVM architecture implemented
   - Project organization and dependencies established
   - Dependency injection framework configured

2. **UI Framework**
   - Main application window with tab-based navigation
   - Basic styling and responsive design
   - View models for main application features

3. **LLM Integration**
   - Multiple LLM provider implementations (OpenAI, Anthropic, Google, Meta, OpenRouter, DeepSeek)
   - Provider-agnostic interface for LLM interactions
   - Model selection and configuration

4. **Voice Services**
   - Basic wake word detection
   - Speech-to-text and text-to-speech interfaces
   - Voice service state management

5. **MCP Server Integration**
   - Server management infrastructure
   - Tool provider registration
   - Basic tool implementations (FileSystem, Calendar, WebSearch, etc.)

#### 2.2.2 Partially Implemented Components

1. **Database Implementation**
   - SQLite database provider
   - Basic repository implementations
   - Database schema defined but not fully implemented

2. **Calendar Integration**
   - Basic Google Calendar integration
   - Calendar tool provider
   - Synchronization infrastructure

3. **Lesson Planning**
   - Basic lesson model
   - Template management
   - UI for lesson creation and editing

4. **Testing Infrastructure**
   - Test organization structure
   - Basic unit tests
   - Test utilities

#### 2.2.3 Components Requiring Implementation

1. **Database Completion**
   - Finalize repository implementations
   - Implement database migrations
   - Complete data validation

2. **External Integrations**
   - Complete fish.audio WebSocket integration
   - Finalize MCP tool implementations
   - Implement Brave Search integration

3. **UI Enhancements**
   - Implement UI improvements from UI_Improvement_Plan.md
   - Complete responsive design
   - Add accessibility features

4. **Testing**
   - Expand unit test coverage
   - Implement integration tests
   - Create UI automated tests

## 3. Implementation Plan

### 3.1 Phase 1: Core Infrastructure Completion (2-3 weeks)

#### 3.1.1 Database Implementation

**Priority: High** (In Progress)

1. **Complete SQLite Implementation** ✅
   - Finalized `SqliteDatabaseProvider` implementation with connection pooling
   - Implemented database connection management
   - Added transaction support

2. **Repository Implementation** ✅
   - Created BaseRepository<T> class with standardized error handling and validation
   - Updated all repositories to use the base class
   - Standardized error handling across repositories
   - Added comprehensive data validation
   - Implemented transaction support for critical operations

3. **Database Migration** ✅
   - Implemented migration mechanism
   - Created migration scripts for schema changes
   - Added database version tracking

4. **Database Backup and Recovery** ✅
   - Implemented automated backup system
   - Added database integrity checking
   - Created recovery mechanisms

#### 3.1.2 System.Text.Json Package Update

**Priority: High**

1. **Update Package References**
   - Update System.Text.Json to latest version
   - Resolve any compatibility issues
   - Update dependent packages

2. **JSON Serialization Refactoring**
   - Update serialization/deserialization code
   - Implement proper error handling
   - Add validation for JSON data

#### 3.1.3 Testing Infrastructure

**Priority: Medium**

1. **Unit Test Expansion**
   - Increase test coverage for core components
   - Implement tests for database operations
   - Add tests for JSON serialization

2. **Test Organization**
   - Complete test organization according to TestOrganization.md
   - Standardize test naming and structure
   - Implement test utilities for common operations

### 3.2 Phase 2: External Integrations (3-4 weeks)

#### 3.2.1 LLM Provider Integrations

**Priority: High**

1. **Provider Implementation Completion**
   - Finalize all LLM provider implementations
   - Add proper error handling and fallback mechanisms
   - Implement model selection and configuration

2. **Tool Integration**
   - Complete LLM tool integration
   - Implement tool call handling
   - Add streaming support for all providers

3. **Testing**
   - Create tests for each LLM provider
   - Test error handling and fallbacks
   - Implement integration tests for LLM services

#### 3.2.2 fish.audio Integration

**Priority: High**

1. **WebSocket Implementation**
   - Implement WebSocket client for fish.audio
   - Add connection management and error handling
   - Create reconnection mechanism

2. **Voice Customization**
   - Implement voice selection
   - Add voice customization parameters
   - Create voice profile management

3. **Phrase Caching**
   - Implement caching for common phrases
   - Add cache management
   - Create cache invalidation mechanism

#### 3.2.3 MCP Server Integrations

**Priority: Medium**

1. **FileSystem Integration**
   - Complete file system operations
   - Implement scratchpad management
   - Add markdown processing

2. **Google Calendar Integration**
   - Implement two-way synchronization
   - Add recurring event support
   - Create calendar sharing features

3. **Brave Search Integration**
   - Implement search API integration
   - Add result filtering and processing
   - Create search history management

4. **Excel Integration**
   - Implement Excel file processing
   - Add data import/export
   - Create template management

5. **Puppeteer Integration**
   - Complete headless browser control
   - Add screenshot and content extraction
   - Implement form filling and navigation

### 3.3 Phase 3: UI Enhancements (2-3 weeks)

#### 3.3.1 UI Styling and Responsiveness

**Priority: Medium**

1. **Style Consistency**
   - Implement consistent styling across all views
   - Add theme support
   - Create responsive layouts

2. **Animation and Transitions**
   - Fix tab transition stuttering
   - Implement smooth animations
   - Add visual feedback for actions

3. **Accessibility**
   - Implement keyboard navigation
   - Add screen reader support
   - Create high-contrast theme

#### 3.3.2 View Implementation Completion

**Priority: Medium**

1. **Lesson Planner View**
   - Complete lesson component editing
   - Implement suggestion generation
   - Add calendar view for scheduling

2. **Class Management View**
   - Complete student information management
   - Implement class scheduling
   - Add data import/export

3. **Configuration View**
   - Complete provider configuration
   - Implement API key management
   - Add system prompt management

4. **System Status View**
   - Implement component health indicators
   - Add log viewing and filtering
   - Create diagnostic tools

### 3.4 Phase 4: Testing and Quality Assurance (2-3 weeks)

#### 3.4.1 Automated Testing

**Priority: High**

1. **Unit Testing**
   - Achieve 80%+ code coverage for core components
   - Implement tests for all repositories
   - Add tests for service implementations

2. **Integration Testing**
   - Create tests for external integrations
   - Implement database integration tests
   - Add API integration tests

3. **UI Testing**
   - Implement automated UI tests
   - Create visual regression tests
   - Add accessibility testing

#### 3.4.2 Manual Testing

**Priority: Medium**

1. **User Testing**
   - Create user testing scripts
   - Implement feedback collection
   - Analyze and address user feedback

2. **Performance Testing**
   - Test application under load
   - Measure response times
   - Identify and fix bottlenecks

3. **Security Testing**
   - Audit API key handling
   - Test authentication mechanisms
   - Verify data protection

## 4. Technical Specifications

### 4.1 Database Design

#### 4.1.1 Schema

The database schema includes the following tables:

1. **Classes**
   - ClassId (TEXT, PK)
   - Name (TEXT)
   - Code (TEXT)
   - Subject (TEXT)
   - YearGroup (TEXT)
   - Notes (TEXT)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

2. **Students**
   - StudentId (TEXT, PK)
   - ClassId (TEXT, FK)
   - Name (TEXT)
   - FsmStatus (INTEGER)
   - SenStatus (INTEGER)
   - EalStatus (INTEGER)
   - AbilityLevel (INTEGER)
   - ReadingAge (TEXT)
   - TargetGrade (TEXT)
   - Notes (TEXT)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

3. **LessonPlans**
   - LessonId (TEXT, PK)
   - ClassId (TEXT, FK)
   - Date (TEXT)
   - TimeSlot (INTEGER)
   - Title (TEXT)
   - LearningObjectives (TEXT)
   - ComponentsJson (TEXT)
   - CalendarEventId (TEXT)
   - SyncStatus (INTEGER)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

4. **LessonResources**
   - ResourceId (TEXT, PK)
   - LessonId (TEXT, FK)
   - Name (TEXT)
   - Type (INTEGER)
   - Path (TEXT)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

5. **LessonTemplates**
   - TemplateId (TEXT, PK)
   - Name (TEXT)
   - Description (TEXT)
   - Category (TEXT)
   - Tags (TEXT)
   - Title (TEXT)
   - LearningObjectives (TEXT)
   - ComponentsJson (TEXT)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

6. **Conversations**
   - ConversationId (TEXT, PK)
   - Title (TEXT)
   - MessagesJson (TEXT)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

7. **SystemPrompts**
   - PromptId (TEXT, PK)
   - Name (TEXT)
   - Content (TEXT)
   - IsDefault (INTEGER)
   - CreatedAt (TEXT)
   - UpdatedAt (TEXT)

8. **DatabaseVersion**
   - Version (INTEGER, PK)
   - AppliedAt (TEXT)

#### 4.1.2 Migrations

Database migrations will be implemented to handle schema changes:

1. **Migration 1**: Initial schema creation
2. **Migration 2**: Add Conversations and SystemPrompts tables
3. **Migration 3**: Add LessonResources and LessonTemplates tables
4. **Migration 4**: Add CalendarEventId and SyncStatus to LessonPlans

#### 4.1.3 Backup and Recovery

The database backup system will include:

1. **Automatic Backups**
   - Daily backups
   - Pre-migration backups
   - Manual backup option

2. **Recovery Mechanisms**
   - Point-in-time recovery
   - Backup restoration
   - Database integrity checking

### 4.2 LLM Integration

#### 4.2.1 Provider Interface

```csharp
public interface ILlmProvider
{
    string ProviderName { get; }
    string ModelName { get; }
    IEnumerable<LlmModel> AvailableModels { get; }
    LlmModel CurrentModel { get; }
    bool RequiresApiKey { get; }
    bool HasValidApiKey { get; }
    bool SupportsStreaming { get; }
    bool SupportsToolCalls { get; }
    bool SupportsVision { get; }

    Task InitializeAsync();
    Task<IEnumerable<LlmModel>> FetchAvailableModelsAsync();
    Task SetApiKeyAsync(string apiKey);
    Task SetModelAsync(string modelId);
    Task<LlmResponse> SendMessageAsync(string message, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task<LlmResponse> SendMessagesAsync(IEnumerable<LlmMessage> messages, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task StreamMessagesAsync(IEnumerable<LlmMessage> messages, Action<string> onToken, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task<LlmResponse> SendMessagesWithToolsAsync(IEnumerable<LlmMessage> messages, IEnumerable<LlmTool> tools, string? systemPrompt = null, CancellationToken cancellationToken = default);
    Task<LlmResponse> SendImageAsync(byte[] imageData, string message, string? systemPrompt = null, CancellationToken cancellationToken = default);
}
```

#### 4.2.2 Provider Implementations

1. **OpenAI Provider**
   - Models: GPT-4o, GPT-4-turbo, GPT-3.5-turbo
   - Features: Streaming, tool calls, vision

2. **Anthropic Provider**
   - Models: Claude 3 Opus, Claude 3 Sonnet, Claude 3 Haiku
   - Features: Streaming, tool calls, vision

3. **Google Provider**
   - Models: Gemini 1.5 Pro, Gemini 1.5 Flash, Gemini 1.0 Pro
   - Features: Streaming, tool calls, vision

4. **Meta Provider**
   - Models: Llama 3 70B, Llama 3 8B
   - Features: Streaming

5. **OpenRouter Provider**
   - Models: Various models from different providers
   - Features: Streaming, tool calls, vision (depending on model)

6. **DeepSeek Provider**
   - Models: DeepSeek Chat, DeepSeek Coder
   - Features: Streaming, tool calls

### 4.3 Voice Services

#### 4.3.1 Wake Word Detection

```csharp
public interface IWakeWordDetector
{
    string WakeWord { get; }
    event EventHandler<WakeWordDetectedEventArgs> WakeWordDetected;
    Task InitializeAsync();
    Task StartListeningAsync();
    Task StopListeningAsync();
}
```

Implementations:
1. **SimpleWakeWordDetector**: Basic keyword spotting
2. **VoskWakeWordDetector**: Offline speech recognition

#### 4.3.2 Speech-to-Text

```csharp
public interface ISpeechToTextProvider
{
    string ProviderName { get; }
    event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
    Task InitializeAsync();
    Task StartListeningAsync();
    Task<(string Text, float Confidence)> StopListeningAsync();
    Task CancelAsync();
    Task<(string Text, float Confidence)> ConvertSpeechToTextAsync(byte[] audioData);
}
```

Implementations:
1. **SimpleSpeechToTextProvider**: Basic implementation
2. **WhisperSpeechToTextProvider**: OpenAI Whisper
3. **GoogleSpeechToTextProvider**: Google Speech-to-Text

#### 4.3.3 Text-to-Speech

```csharp
public interface ITextToSpeechProvider
{
    string ProviderName { get; }
    Task InitializeAsync();
    Task<byte[]> ConvertTextToSpeechAsync(string text, CancellationToken cancellationToken = default);
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);
    Task CancelSpeechAsync();
}
```

Implementations:
1. **SimpleTextToSpeechProvider**: Basic implementation
2. **FishAudioTextToSpeechProvider**: fish.audio WebSocket
3. **OpenAiTextToSpeechProvider**: OpenAI TTS
4. **GoogleTextToSpeechProvider**: Google Text-to-Speech

### 4.4 MCP Server Integration

#### 4.4.1 Server Management

```csharp
public interface IMcpServerManager
{
    string ServerUrl { get; }
    bool IsServerRunning { get; }
    event EventHandler<ServerStatusChangedEventArgs> ServerStatusChanged;
    Task StartServerAsync();
    Task StopServerAsync();
    Task RestartServerAsync();
    void RegisterToolProvider(IMcpToolProvider provider);
    Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters);
    IEnumerable<McpToolSchema> GetToolSchema();
}
```

#### 4.4.2 Tool Providers

```csharp
public interface IMcpToolProvider
{
    string ProviderName { get; }
    IEnumerable<IMcpTool> Tools { get; }
    Task InitializeAsync();
    IMcpTool? GetTool(string toolName);
    Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters);
    IEnumerable<McpToolSchema> GetToolSchema();
}
```

Implementations:
1. **FileSystemToolProvider**: File operations
2. **CalendarToolProvider**: Google Calendar integration
3. **WebSearchToolProvider**: Brave Search integration
4. **ExcelToolProvider**: Excel file processing
5. **FetchToolProvider**: Web content fetching
6. **SequentialThinkingToolProvider**: Reasoning tools
7. **PuppeteerToolProvider**: Headless browser control

### 4.5 UI Design

#### 4.5.1 Main Window Layout

The main window uses a tab-based layout with the following tabs:
1. **Home**: Dashboard with quick access to features
2. **Chat**: Conversation interface with the LLM
3. **Classes**: Class and student management
4. **Lesson Planner**: Lesson creation and scheduling
5. **Configuration**: Application settings
6. **System Status**: Diagnostics and monitoring
7. **Notifications**: System notifications

#### 4.5.2 View Models

```csharp
public class MainViewModel : ViewModelBase
{
    public HomeViewModel HomeViewModel { get; }
    public ChatViewModel ChatViewModel { get; }
    public ClassViewModel ClassViewModel { get; }
    public LessonPlannerViewModel LessonPlannerViewModel { get; }
    public ConfigurationViewModel ConfigurationViewModel { get; }
    public SystemStatusViewModel SystemStatusViewModel { get; }
    public NotificationsViewModel NotificationsViewModel { get; }
    public int SelectedTabIndex { get; set; }
}
```

#### 4.5.3 UI Improvements

Based on the UI_Improvement_Plan.md:

1. **Critical Fixes**
   - Fix subtle focus indicators
   - Implement confirmation dialogs
   - Fix tab transition stuttering

2. **User Experience Improvements**
   - Adjust notification timing
   - Implement progress indicators
   - Fix form alignment

3. **Advanced Features**
   - Implement theme switcher
   - Add keyboard shortcuts
   - Create help system

## 5. Testing Strategy

### 5.1 Unit Testing

Unit tests will focus on testing individual components in isolation:

1. **Core Models and Interfaces**
   - Test model validation
   - Test interface implementations
   - Test utility functions

2. **Services**
   - Test service implementations with mocked dependencies
   - Test error handling
   - Test edge cases

3. **Repositories**
   - Test CRUD operations
   - Test data validation
   - Test transaction handling

### 5.2 Integration Testing

Integration tests will verify that components work together correctly:

1. **Database Integration**
   - Test repository interactions with the database
   - Test migrations
   - Test backup and recovery

2. **External API Integration**
   - Test LLM provider integrations
   - Test Google Calendar integration
   - Test Brave Search integration

3. **MCP Server Integration**
   - Test tool provider integrations
   - Test server management
   - Test tool execution

### 5.3 UI Testing

UI testing will ensure that the user interface works correctly:

1. **Automated UI Tests**
   - Test navigation
   - Test form submission
   - Test data display

2. **Manual Testing**
   - Follow test scripts from UI_Testing_Checklist.md
   - Collect feedback using UI_Testing_Feedback_Form.md
   - Document results in UI_Testing_Results.md

### 5.4 Performance Testing

Performance testing will ensure that the application performs well:

1. **Response Time Testing**
   - Measure UI responsiveness
   - Test LLM response times
   - Test database query performance

2. **Resource Usage Testing**
   - Monitor memory usage
   - Test CPU utilization
   - Measure disk I/O

## 6. Deployment and Maintenance

### 6.1 Deployment

The application will be deployed as a Windows desktop application:

1. **Installation Package**
   - Create installer using WiX Toolset
   - Include all dependencies
   - Configure automatic updates

2. **Configuration**
   - Create default configuration
   - Allow user customization
   - Implement configuration backup

### 6.2 Maintenance

Ongoing maintenance will include:

1. **Updates**
   - Implement update mechanism
   - Create update notification
   - Allow automatic updates

2. **Logging**
   - Implement comprehensive logging
   - Create log rotation
   - Add log analysis tools

3. **Error Reporting**
   - Implement error reporting
   - Create crash dumps
   - Add telemetry (opt-in)

## 7. Implementation Progress

### 7.1 Phase 1: Core Infrastructure Completion

#### 7.1.1 Database Implementation

**Status: In Progress**

**Completed Tasks:**
- Enhanced SqliteDatabaseProvider with connection pooling and proper error handling
- Implemented comprehensive database migration system
- Created DatabaseMigrations class with all required schema changes
- Added transaction support for database operations
- Implemented database backup and recovery mechanisms
- Created BaseRepository<T> class with standardized error handling and validation
- Updated all repositories to use the BaseRepository class with improved error handling and validation
- Standardized error handling across all repositories
- Added comprehensive data validation in all repositories
- Implemented transaction support for critical operations
- Implemented unit tests for BaseRepository and all repository implementations
- Implemented integration tests for repository operations

**Next Steps:**
- Implement remaining repository classes (if needed)
- Enhance error handling for edge cases
- Optimize database queries for performance

### 7.2 Remaining Phases

After completing Phase 1, we will proceed with:
- Phase 2: External Integrations (LLM providers, fish.audio, MCP tools)
- Phase 3: UI Enhancements (styling, responsiveness, view implementations)
- Phase 4: Testing and Quality Assurance

## 8. Conclusion

This Design Document provides a comprehensive plan for completing the ADEPT AI Teaching Assistant. By following this plan, the development team can systematically implement the remaining components, integrate external services, enhance the user interface, and ensure the quality of the application.

The phased approach allows for incremental development and testing, with each phase building on the previous one. The technical specifications provide detailed guidance for implementation, while the testing strategy ensures that the application meets the requirements and provides a high-quality user experience.

With this plan, the ADEPT project can move forward with a clear direction and a solid foundation for success.

The implementation of Phase 1.1 (Database Implementation) has already begun, with significant progress made on the database provider, migration system, and backup/recovery mechanisms. This provides a solid foundation for the remaining database work and subsequent phases.
