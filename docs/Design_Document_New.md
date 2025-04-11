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

**Priority: High** ✅

1. **Update Package References** ✅
   - Update System.Text.Json to latest version
   - Resolve any compatibility issues
   - Update dependent packages

2. **JSON Serialization Refactoring** ✅
   - Update serialization/deserialization code
   - Implement proper error handling
   - Add validation for JSON data
   - Create standardized JSON serialization helpers
   - Implement extension methods for easier JSON operations

#### 3.1.3 Testing Infrastructure

**Priority: Medium** ✅

1. **Test Utilities Enhancement** ✅
   - Enhanced MockFactory with mock implementations for all services and repositories ✅
   - Added test data generators for all model types ✅
   - Implemented additional assertion extensions for common test scenarios ✅
   - Created test fixtures for different testing scenarios ✅
   - Fixed ambiguous references to MockFactory in test utilities ✅
   - Updated test utilities to match current database interfaces ✅

2. **Unit Test Expansion** ✅
   - Increased test coverage for core components ✅
   - Implement tests for database operations ✅
   - Implement tests for repositories (LessonResourceRepository, LessonTemplateRepository, SystemPromptRepository) ✅
   - Fixed repository tests to use the BaseRepository pattern correctly ✅
   - Fixed nullability reference type issues in repository tests ✅
   - Fixed dynamic operations in expression trees ✅
   - Fixed EntityValidator mocking issues in tests ✅
   - Add tests for JSON serialization ✅
   - Add tests for JSON validation ✅
   - Add tests for JSON extension methods ✅
   - Tests now build successfully with warnings but no errors ✅
   - Fixed repository delete operations to return Task<bool> instead of Task ✅
   - Updated repository interfaces to match implementation return types ✅
   - Fixed test expectations for delete operations to match new behavior ✅
   - Fixed database backup and integrity service tests ✅
   - All tests now pass successfully ✅

3. **Remaining Test Issues** ✅
   - Resolved ambiguous references to MockFactory in LlmServiceFallbackTests.cs ✅
   - Fixed issues with ReturnsAsync in LlmToolIntegrationServiceTests.cs ✅
   - Addressed nullability warnings in OpenRouterProviderTests.cs ✅
   - Ensured all tests build and run successfully ✅

4. **Integration Test Development** (In Progress)
   - Create integration tests for database operations ✅
   - Implement tests for external service integrations
   - Add end-to-end tests for critical workflows

4. **Test Organization** ✅
   - Complete test organization according to TestOrganization.md ✅
   - Standardize test naming and structure ✅
   - Implement test utilities for common operations ✅
   - Create consistent test project structure ✅

### 3.2 Phase 2: External Integrations (3-4 weeks)

#### 3.2.1 LLM Provider Integrations

**Priority: High** (In Progress)

1. **Provider Implementation Completion** (In Progress)
   - Finalized OpenAI and Anthropic provider implementations ✅
   - Implemented OpenRouterProvider with full ILlmProvider interface support ✅
   - Added proper error handling and logging in OpenRouterProvider ✅
   - Fixed issues in LlmService related to conversation handling ✅
   - Fixed boolean comparison in model capability detection ✅
   - Implemented model selection and configuration in OpenRouterProvider ✅
   - Added support for streaming, tool calls, and vision capabilities in OpenRouterProvider ✅
   - Fixed test project build issues related to LLM providers ✅
   - Implemented Google (Gemini), Meta (Llama), and DeepSeek providers with latest models ✅
   - Added vision support to all providers that support it ✅

2. **Model Refresh Enhancement** ✅
   - Implemented periodic model refresh mechanism in LlmService ✅
   - Added timer-based automatic refresh for providers with valid API keys ✅
   - Enhanced model selection logic to prefer newer models ✅
   - Added intelligent model upgrade to automatically select newer model variants ✅
   - Improved model capability detection (streaming, tool calls, vision) ✅

3. **Tool Integration** ✅
   - Complete LLM tool integration ✅
   - Implement tool call handling ✅
   - Add streaming support for all providers ✅
   - Enhance tool call processing with parallel execution ✅
   - Add streaming support for tool calls ✅

4. **Testing** ✅
   - Created tests for model refresh functionality ✅
   - Created tests for tool integration functionality ✅
   - Test error handling and fallbacks ✅
   - Test model refresh functionality ✅
   - Implement integration tests for LLM services ✅

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
   - Models: Various models from different providers including Claude, GPT, Llama, Mistral, and more
   - Features: Streaming, tool calls, vision (depending on model)
   - Implementation complete with full ILlmProvider interface support

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

**Status: Completed**

**Completed Tasks:**
- Enhanced SqliteDatabaseProvider with connection pooling and proper error handling
- Implemented comprehensive database migration system
- Created DatabaseMigrations class with all required schema changes
- Added transaction support for database operations
- Implemented database backup and recovery mechanisms
- Created BaseRepository<T> class with standardized error handling and validation
- Updated all repositories to use the BaseRepository class with improved error handling and validation:
  - ClassRepository
  - StudentRepository
  - LessonRepository
  - ConversationRepository
  - SystemPromptRepository
  - LessonResourceRepository
  - LessonTemplateRepository
- Standardized error handling across all repositories
- Added comprehensive data validation in all repositories
- Implemented transaction support for critical operations
- Enhanced error handling for edge cases
- Optimized database queries for performance
- Added proper parameter validation in all repository methods
- Implemented consistent error messages with context
- Added validation for entity existence before updates/deletes
- Standardized SQL queries with consistent naming conventions

**Key Benefits:**
- Improved data integrity through consistent validation and error handling
- Enhanced performance through optimized database operations
- Reduced code duplication through the BaseRepository pattern
- Better maintainability through standardized approaches
- Improved error reporting for easier debugging

#### 7.1.2 System.Text.Json Package Update

**Status: Completed**

**Completed Tasks:**
- Updated System.Text.Json to latest version (8.0.5)
- Created JsonSerializerOptionsFactory for standardized JSON serialization options with different presets:
  - Default options with camelCase property naming and enum string conversion
  - Indented options for human-readable JSON output
  - API request/response options for external service integration
  - File storage options for persistent data
- Created JsonHelper class for standardized JSON serialization and deserialization with:
  - Consistent error handling
  - Null-safe operations
  - Validation capabilities
- Implemented proper error handling with custom exception classes:
  - JsonValidationException
  - JsonSerializationException
  - JsonDeserializationException
- Added validation for JSON data with custom validation attributes:
  - ValidJsonAttribute
  - ValidJsonObjectAttribute<T>
- Created extension methods for easier JSON operations:
  - ToJson() / FromJson()
  - TryFromJson()
  - IsValidJson() / ValidateJson()
  - DeepClone() / ConvertTo<T>()
- Updated model classes to use the new JSON helpers:
  - Conversation
  - LessonPlan
  - LessonTemplate
  - SystemPrompt (moved to separate file)
- Added unit tests for JSON serialization and deserialization
- Added validation for JSON content in models

**Key Benefits:**
- Standardized approach to JSON serialization across the application
- Improved error handling for JSON operations
- Enhanced validation for JSON data
- Better maintainability through consistent patterns
- Simplified model classes with cleaner JSON handling
- Improved testability with dedicated test classes

#### 7.1.3 Testing Infrastructure

**Status: Completed**

**Completed Tasks:**
- Implemented unit tests for BaseRepository
- Implemented unit tests for repository implementations
- Implemented integration tests for repository operations
- Added tests for JSON serialization and deserialization
- Added tests for JSON validation
- Added tests for JSON extension methods
- Created test project structure:
  - Unit tests for each project
  - Integration tests for database operations
  - Common test utilities
- Fixed repository delete operations to return Task<bool> instead of Task
- Updated repository interfaces to match implementation return types
- Fixed test expectations for delete operations to match new behavior
- Fixed database backup and integrity service tests
- All tests now pass successfully

**Planned Tasks:**
- Complete test organization according to TestOrganization.md
- Standardize test naming and structure
- Add more integration tests for external services
- Implement automated test runs in CI/CD pipeline

### 7.2 Phase 2: External Integrations

**Status: In Progress**

#### 7.2.1 LLM Provider Integrations

**Completed Tasks:**
- Enhanced OpenRouterProvider implementation:
  - Implemented all ILlmProvider interface methods and properties
  - Added proper error handling and logging
  - Implemented model selection and configuration
  - Added support for streaming, tool calls, and vision capabilities
  - Fixed build issues related to the implementation
- Fixed issues in LlmService related to conversation handling
- Fixed boolean comparison in model capability detection

**Completed Action Plan:** ✅
- Fixed test project build issues: ✅
  - Resolved ambiguous references to MockFactory in LlmServiceFallbackTests.cs ✅
  - Fixed issues with ReturnsAsync in LlmToolIntegrationServiceTests.cs ✅
  - Addressed nullability warnings in OpenRouterProviderTests.cs ✅
  - Ensured all tests build and run successfully ✅

**Completed Tasks:** ✅
- Enhanced remaining LLM provider implementations: ✅
  - Google (Gemini) with latest models and vision support ✅
  - Meta (Llama) with latest models and vision support ✅
  - DeepSeek with latest models and vision support ✅
- Updated model lists with the latest available models for all providers ✅

**Next Planned Tasks:**
- Implement fish.audio as the default TTS provider via WebSocket
  - Add voice customization options
  - Implement error handling and fallback mechanisms
  - Add phrase caching for improved performance
  - Add refresh on application startup
  - Add refresh when API keys are updated
- Enhance model selection logic to prefer newer models
- Implement provider selection and fallback mechanisms
- Add proper error handling and retry logic
- Create comprehensive tests for all LLM providers

#### 7.2.2 Fish.Audio Integration

**Planned Tasks:**
- Implement Fish.Audio as the default TTS provider
- Create WebSocket client for real-time audio streaming
- Implement voice customization options
- Add phrase caching for improved performance
- Implement error handling and fallback mechanisms

#### 7.2.3 MCP Server Integrations

**Planned Tasks:**
- Implement File System operations
- Implement Google Calendar integration
- Implement Brave Search integration
- Implement Excel integration
- Implement Fetch functionality
- Implement Sequential Thinking
- Implement Puppeteer integration

### 7.3 Phase 3: UI Enhancements

**Status: Planned**

**Planned Tasks:**
- Enhance UI styling
- Implement responsive design
- Add visual feedback for user actions
- Implement accessibility features
- Complete all view implementations
- Add user preference settings

### 7.4 Phase 4: Testing and Quality Assurance

**Status: Planned**

**Planned Tasks:**
- Conduct comprehensive testing
- Perform user acceptance testing
- Optimize performance
- Enhance error handling
- Improve documentation
- Prepare for deployment

## 8. Conclusion

This Design Document provides a comprehensive plan for completing the ADEPT AI Teaching Assistant. By following this plan, the development team can systematically implement the remaining components, integrate external services, enhance the user interface, and ensure the quality of the application.

The phased approach allows for incremental development and testing, with each phase building on the previous one. The technical specifications provide detailed guidance for implementation, while the testing strategy ensures that the application meets the requirements and provides a high-quality user experience.

With this plan, the ADEPT project can move forward with a clear direction and a solid foundation for success.

The ADEPT project has made significant progress with the completion of Phase 1.1 (Database Implementation) and Phase 1.2 (System.Text.Json Package Update). These phases have established a solid foundation for the application:

1. **Database Infrastructure**: We've implemented a robust database layer with standardized repositories, comprehensive error handling, and optimized queries. The BaseRepository pattern has significantly reduced code duplication while improving maintainability and reliability.

2. **JSON Serialization**: We've created a standardized approach to JSON handling with the latest System.Text.Json package. The new JSON helpers provide consistent error handling, validation, and simplified operations across the application.

3. **Testing Framework**: We've completed building a comprehensive testing infrastructure with unit tests for repositories and JSON operations, as well as integration tests for database functionality. All tests now pass successfully, providing confidence in the reliability of the core infrastructure.

We have now completed Phase 1.3 (Testing Infrastructure), ensuring the application has comprehensive test coverage and a standardized approach to testing. We have also completed Phase 2.1 (LLM Provider Integrations) with the implementation of all planned LLM providers: OpenAI, Anthropic, OpenRouter, Google (Gemini), Meta (Llama), and DeepSeek. Each provider now fully supports the ILlmProvider interface with streaming, tool calls, and vision capabilities where applicable.

All provider implementations include proper error handling, logging, model selection, and configuration. We've also fixed related issues in the LlmService class, including conversation handling and model capability detection. The model lists for all providers have been updated to include the latest available models, ensuring that users have access to the most advanced AI capabilities.

We have successfully addressed the test project build issues, including resolving ambiguous references to MockFactory, fixing issues with ReturnsAsync in test mocks, and addressing nullability warnings. This ensures a solid testing foundation for the remaining implementation work.

Our next focus is implementing fish.audio as the default TTS provider via WebSocket, which will enhance the application's voice capabilities with customizable voices, error handling, and performance optimizations through phrase caching.

By following this phased approach and prioritizing quality, we're systematically addressing the core infrastructure needs before moving on to additional external integrations and UI enhancements. This ensures that each component is built on a solid foundation, making the application more reliable, maintainable, and extensible.
