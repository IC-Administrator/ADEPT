# Design Document for Adept AI Teaching Assistant

## 1. Introduction and Document Overview

### 1.1 Purpose

This Design Document provides comprehensive technical specifications and implementation guidelines for the Adept AI Teaching Assistant application. It serves as a bridge between the requirements phase (SRS, URS, SysRS) and the implementation phase, offering detailed instructions for the AI coding assistant to build the system.

### 1.2 Scope

This document covers the complete technical design of the Adept AI Teaching Assistant, including:

- System architecture and component design
- Database implementation details
- User interface specifications
- Integration methods for external services
- Security mechanisms
- Error handling and recovery strategies
- Performance considerations
- Implementation guidance

### 1.3 Intended Audience

This document is primarily intended for:
- AI coding assistants implementing the system
- Developers enhancing or maintaining the codebase
- Technical reviewers evaluating the design

### 1.4 Design Approach

The design follows these key principles:
- Modular architecture with clear separation of concerns
- Asynchronous processing for responsive UI
- Graceful degradation when services fail
- Security by design
- Extensibility for future enhancements

## 2. System Architecture Overview

### 2.1 Architectural Pattern

The Adept AI Teaching Assistant will implement a modified Model-View-ViewModel (MVVM) architecture with service layers for external integrations. This pattern provides:

- Clear separation between UI (View), application logic (ViewModel), and data (Model)
- Testability through decoupled components
- Support for asynchronous operations
- Maintainability through well-defined interfaces

### 2.2 High-Level Architecture Diagram

The system will consist of the following high-level components:

```
┌───────────────────────────────────────────────────────────────────────┐
│                           User Interface Layer                         │
│  ┌─────────────┐  ┌────────────────┐  ┌────────────┐  ┌────────────┐  │
│  │ Config View │  │ Class Mgmt View│  │Lesson View │  │ Status View│  │
│  └─────────────┘  └────────────────┘  └────────────┘  └────────────┘  │
└───────────────────────────────────┬───────────────────────────────────┘
                                    │
┌───────────────────────────────────▼───────────────────────────────────┐
│                          ViewModel Layer                               │
│  ┌─────────────┐  ┌────────────────┐  ┌────────────┐  ┌────────────┐  │
│  │ Config VM   │  │ Class Mgmt VM  │  │ Lesson VM  │  │ Status VM  │  │
│  └─────────────┘  └────────────────┘  └────────────┘  └────────────┘  │
└───────────────────────────────────┬───────────────────────────────────┘
                                    │
┌───────────────────────────────────▼───────────────────────────────────┐
│                           Service Layer                                │
│  ┌─────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────────┐  │
│  │Voice Service│  │ LLM Service│  │Data Service│  │External Service│  │
│  └─────────────┘  └────────────┘  └────────────┘  └────────────────┘  │
└───────────────────────────────────┬───────────────────────────────────┘
                                    │
┌───────────────────────────────────▼───────────────────────────────────┐
│                            Data Layer                                  │
│  ┌─────────────┐  ┌────────────────┐  ┌────────────────────────────┐  │
│  │SQLite DB    │  │File System     │  │External APIs               │  │
│  └─────────────┘  └────────────────┘  └────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────┘
```

### 2.3 Component Communication

Components will communicate through:
- Property binding for UI updates (MVVM pattern)
- Events and callbacks for asynchronous operations
- Service interfaces for cross-component functionality
- Repository pattern for data access

### 2.4 Technology Stack

The application will be built using:

- **Programming Language**: C# with .NET 6.0+
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with Microsoft.Data.Sqlite
- **API Communication**: HttpClient and WebSocketClient
- **JSON Processing**: System.Text.Json
- **Encryption**: .NET Cryptography libraries
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Serilog
- **Audio Processing**: NAudio library
- **Configuration**: Microsoft.Extensions.Configuration

## 3. Detailed Component Design

### 3.1 User Interface Layer

#### 3.1.1 Main Application Window

The main application window will use a tabbed interface with the following structure:

```
┌─────────────────────────────────────────────────────────────────┐
│ Adept AI Teaching Assistant                              _ □ X  │
├─────┬─────────┬───────────────┬───────────────┬────────────────┤
│Home │ Classes │ Lesson Planner│ Configuration │ System Status  │
├─────┴─────────┴───────────────┴───────────────┴────────────────┤
│                                                                 │
│                     [Selected Tab Content]                      │
│                                                                 │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ Voice Status: [Indicator]    Current Lesson: [Info if active]   │
└─────────────────────────────────────────────────────────────────┘
```

The UI will implement responsive design principles to accommodate different screen sizes and resolutions (minimum 1366x768).

#### 3.1.2 Home Tab

The Home tab will provide:
- Current system status overview
- Quick access to common functions
- Voice interaction history
- Current/upcoming lesson information
- Recent notifications

#### 3.1.3 Classes Tab

The Classes tab will contain:
- List of classes with basic information
- Detailed class view showing student information
- Class editing capabilities
- Excel import functionality
- Teaching schedule configuration

#### 3.1.4 Lesson Planner Tab

The Lesson Planner tab will include:
- Calendar view for lesson scheduling
- Lesson template creation and management
- Lesson component editing (retrieval questions, activities, etc.)
- Google Calendar synchronization status
- Suggestion generation controls

#### 3.1.5 Configuration Tab

The Configuration tab will provide:
- STT provider selection and configuration
- LLM provider selection and configuration
- TTS settings configuration
- API key management
- System prompt management
- MCP server configuration
- Scratchpad folder selection

#### 3.1.6 System Status Tab

The System Status tab will display:
- Component health indicators
- API call statistics
- Error logs
- Resource usage
- Diagnostic tools

#### 3.1.7 Voice Status Indicator

A persistent status bar will show:
- Current listening state (idle, listening, processing)
- Visual feedback during voice activation
- Current lesson information if active
- Quick access to voice controls

### 3.2 ViewModel Layer

#### 3.2.1 MainViewModel

- Manages navigation between tabs
- Coordinates cross-component communication
- Initializes and manages services
- Handles application lifecycle events

#### 3.2.2 ConfigurationViewModel

- Manages provider configuration settings
- Handles API key validation and storage
- Controls system prompt management
- Manages MCP server configuration

#### 3.2.3 ClassManagementViewModel

- Handles class data CRUD operations
- Manages student information
- Processes Excel imports
- Controls teaching schedule configuration

#### 3.2.4 LessonPlannerViewModel

- Manages lesson creation and editing
- Handles Google Calendar synchronization
- Processes lesson component suggestions
- Controls calendar view and navigation

#### 3.2.5 StatusViewModel

- Collects and displays system health information
- Manages log display and filtering
- Handles diagnostic functions
- Monitors resource usage

#### 3.2.6 VoiceStatusViewModel

- Manages voice activation state
- Handles transcription display
- Controls audio feedback
- Monitors current lesson awareness

### 3.3 Service Layer

#### 3.3.1 VoiceService

**Wake Word Detection Component**:
- Implements continuous audio monitoring
- Uses lightweight keyword detection algorithm
- Minimizes CPU usage during idle listening
- Triggers activation on wake word detection

**Speech-to-Text Component**:
- Manages provider-specific API calls
- Handles audio capture and processing
- Implements error handling for failed transcriptions
- Optimizes for educational terminology

**Text-to-Speech Component**:
- Implements WebSocket communication with fish.audio
- Manages custom voice model usage
- Provides fallback TTS capabilities
- Controls audio output quality and pacing

#### 3.3.2 LLMService

**Provider Management Component**:
- Handles multiple LLM provider integrations
- Manages API authentication and rate limiting
- Implements provider-specific request formatting
- Handles provider switching and fallback

**Context Management Component**:
- Builds appropriate context for LLM requests
- Maintains conversation history
- Incorporates class and lesson information
- Optimizes context length for different providers

**System Prompt Component**:
- Applies user-defined system prompts
- Manages prompt templates and variables
- Handles prompt switching based on context
- Optimizes prompt effectiveness

**Tool Integration Component**:
- Detects tool requests in LLM responses
- Routes requests to appropriate MCP servers
- Formats inputs according to MCP requirements
- Processes and formats tool outputs

#### 3.3.3 DataService

**Database Component**:
- Implements SQLite database operations
- Manages database connections and transactions
- Handles data validation and integrity
- Provides repository interfaces for data access

**File System Component**:
- Manages scratchpad folder operations
- Handles file reading and writing
- Implements file permissions and security
- Processes markdown and other file formats

**Class Information Component**:
- Manages class and student data
- Processes Excel imports
- Handles teaching schedule information
- Provides data for LLM context

**Lesson Planning Component**:
- Manages lesson plan data
- Handles lesson component organization
- Associates lessons with classes and time slots
- Tracks Google Calendar synchronization

#### 3.3.4 ExternalService

**MCP Server Component**:
- Initializes and manages MCP server connections
- Routes tool requests to appropriate servers
- Handles server errors and fallbacks
- Implements standardized request/response formatting

**Google Calendar Component**:
- Handles OAuth authentication
- Manages calendar event creation and updates
- Synchronizes lesson plans with calendar events
- Implements rate limiting and error handling

**Web Search Component**:
- Interfaces with Brave Search API
- Processes and formats search results
- Implements retry logic and rate limiting
- Optimizes search queries for educational context

**Excel Processing Component**:
- Reads and parses Excel files
- Extracts structured data from standardized formats
- Validates imported data
- Converts data to internal representations

### 3.4 Data Layer

#### 3.4.1 Database Schema Implementation

The SQLite database will implement the schema defined in the SRS with the following optimizations:

- Foreign key constraints for referential integrity
- Appropriate indexes for common query patterns
- Normalization for data consistency
- Transaction support for critical operations

#### 3.4.2 File System Organization

The file system will be organized as follows:

```
[Application Directory]/
├── Adept.exe                 # Main executable
├── config/                   # Configuration files
│   ├── settings.json         # General settings
│   ├── providers.json        # Provider configurations
│   └── prompts/              # System prompts
├── data/                     # Application data
│   └── adept.db              # SQLite database
├── logs/                     # Log files
├── lib/                      # Application libraries
│   ├── mcp/                  # MCP server executables
│   └── dependencies/         # Other dependencies
└── [User-Selected Scratchpad Directory]/
    ├── lessons/              # Lesson resources
    ├── generated/            # AI-generated content
    └── temp/                 # Temporary files
```

#### 3.4.3 External API Interfaces

**STT Provider Interface**:
- Implements provider-specific authentication
- Handles audio format conversions
- Manages request/response formatting
- Implements error handling and retries

**LLM Provider Interface**:
- Implements provider-specific request formats
- Manages context windows and limitations
- Handles streaming responses when available
- Implements token counting and optimization

**fish.audio WebSocket Interface**:
- Implements WebSocket connection management
- Handles authentication and session maintenance
- Processes audio streaming
- Manages connection errors and reconnection

**Google Calendar REST Interface**:
- Implements OAuth 2.0 authentication flow
- Manages token refresh and storage
- Handles calendar CRUD operations
- Implements query optimization and caching

**Brave Search API Interface**:
- Implements authentication and request formatting
- Processes search results and pagination
- Manages rate limiting and quotas
- Optimizes query construction

## 4. Database Design

### 4.1 Database Technology Selection

The application will use SQLite as the database engine based on:
- Self-contained operation without separate server process
- Zero-configuration deployment
- Cross-platform compatibility
- Reliable transaction support
- Appropriate performance for single-user application
- Small footprint and resource requirements

The application will use Microsoft.Data.Sqlite for database access, providing:
- Modern async/await API support
- Integration with .NET data patterns
- Transaction management
- Parameter binding for query safety

### 4.2 Detailed Schema Design

The database will implement the following tables and relationships:

#### 4.2.1 Configuration Table

```sql
CREATE TABLE Configuration (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    encrypted INTEGER NOT NULL DEFAULT 0,
    last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

This table will store:
- Application settings
- Default provider selections
- UI preferences
- Non-sensitive configuration values

#### 4.2.2 SecureStorage Table

```sql
CREATE TABLE SecureStorage (
    key TEXT PRIMARY KEY,
    encrypted_value BLOB NOT NULL,
    iv BLOB NOT NULL,
    last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

This table will store:
- API keys
- OAuth tokens
- Other sensitive credentials
- All values will be encrypted using AES-256

#### 4.2.3 Classes Table

```sql
CREATE TABLE Classes (
    class_id TEXT PRIMARY KEY,
    class_code TEXT NOT NULL,
    education_level TEXT NOT NULL,
    current_topic TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_classes_code ON Classes(class_code);
```

#### 4.2.4 Students Table

```sql
CREATE TABLE Students (
    student_id TEXT PRIMARY KEY,
    class_id TEXT NOT NULL,
    name TEXT NOT NULL,
    fsm_status INTEGER,
    sen_status INTEGER,
    eal_status INTEGER,
    ability_level TEXT,
    reading_age TEXT,
    target_grade TEXT,
    notes TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE CASCADE
);

CREATE INDEX idx_students_class ON Students(class_id);
```

#### 4.2.5 TeachingSchedule Table

```sql
CREATE TABLE TeachingSchedule (
    schedule_id TEXT PRIMARY KEY,
    day_of_week INTEGER NOT NULL,
    time_slot INTEGER NOT NULL,
    class_id TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX idx_schedule_slot ON TeachingSchedule(day_of_week, time_slot);
CREATE INDEX idx_schedule_class ON TeachingSchedule(class_id);
```

#### 4.2.6 LessonPlans Table

```sql
CREATE TABLE LessonPlans (
    lesson_id TEXT PRIMARY KEY,
    class_id TEXT NOT NULL,
    date TEXT NOT NULL,
    time_slot INTEGER NOT NULL,
    title TEXT NOT NULL,
    learning_objectives TEXT,
    calendar_event_id TEXT,
    components JSON NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX idx_lesson_class_date_slot ON LessonPlans(class_id, date, time_slot);
CREATE INDEX idx_lesson_date ON LessonPlans(date);
CREATE INDEX idx_lesson_calendar ON LessonPlans(calendar_event_id);
```

The `components` JSON field will store structured data for:
- Retrieval questions (with answers)
- Challenge question (with answer)
- Big question
- Starter activity
- Main activity
- Plenary activity

#### 4.2.7 Conversations Table

```sql
CREATE TABLE Conversations (
    conversation_id TEXT PRIMARY KEY,
    class_id TEXT,
    date TEXT NOT NULL,
    time_slot INTEGER,
    history JSON NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (class_id) REFERENCES Classes(class_id) ON DELETE SET NULL
);

CREATE INDEX idx_conversation_class ON Conversations(class_id);
CREATE INDEX idx_conversation_date ON Conversations(date);
```

The `history` JSON field will store:
- User commands
- Assistant responses
- Timestamps
- Associated lesson phase (if applicable)

#### 4.2.8 SystemPrompts Table

```sql
CREATE TABLE SystemPrompts (
    prompt_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    content TEXT NOT NULL,
    is_default INTEGER NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_prompts_default ON SystemPrompts(is_default);
```

### 4.3 Data Access Layer

The application will implement a Repository pattern for data access with:

- Repository interfaces for each entity type
- Concrete SQLite implementations
- Async methods for database operations
- Unit of Work pattern for transaction management
- Entity mapping between database and domain objects

Example implementation for the ClassRepository:

```csharp
public interface IClassRepository
{
    Task<IEnumerable<Class>> GetAllClassesAsync();
    Task<Class> GetClassByIdAsync(string classId);
    Task<Class> GetClassByCodeAsync(string classCode);
    Task<string> AddClassAsync(Class classEntity);
    Task UpdateClassAsync(Class classEntity);
    Task DeleteClassAsync(string classId);
}

public class SqliteClassRepository : IClassRepository
{
    private readonly DatabaseContext _dbContext;
    
    public SqliteClassRepository(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<IEnumerable<Class>> GetAllClassesAsync()
    {
        const string sql = "SELECT * FROM Classes ORDER BY class_code";
        return await _dbContext.QueryAsync<Class>(sql);
    }
    
    // Other method implementations...
}
```

### 4.4 Data Migration Strategy

The application will implement a version-based migration strategy:

- Database version tracking in a special table
- Incremental migration scripts for version updates
- Automatic migration on application startup
- Backup before migration for recovery

## 5. Security Design

### 5.1 API Key Management

The application will implement secure API key storage:

1. Keys will be encrypted using AES-256 encryption
2. Encryption key will be derived from the Windows Data Protection API (DPAPI)
3. Encrypted keys will be stored in the SecureStorage table
4. Keys will be decrypted only when needed for API calls
5. Keys will never be logged or displayed in plain text

Implementation approach:

```csharp
public class SecureStorageService : ISecureStorageService
{
    private readonly IRepository<SecureStorage> _repository;
    private readonly ICryptographyService _cryptoService;
    
    public SecureStorageService(IRepository<SecureStorage> repository, 
                               ICryptographyService cryptoService)
    {
        _repository = repository;
        _cryptoService = cryptoService;
    }
    
    public async Task StoreSecureValueAsync(string key, string value)
    {
        var (encryptedValue, iv) = _cryptoService.Encrypt(value);
        
        await _repository.AddOrUpdateAsync(new SecureStorage
        {
            Key = key,
            EncryptedValue = encryptedValue,
            Iv = iv,
            LastModified = DateTime.UtcNow
        });
    }
    
    public async Task<string> RetrieveSecureValueAsync(string key)
    {
        var secureItem = await _repository.GetByKeyAsync(key);
        if (secureItem == null) return null;
        
        return _cryptoService.Decrypt(secureItem.EncryptedValue, secureItem.Iv);
    }
}
```

### 5.2 OAuth Implementation

For Google Calendar authentication, the application will implement:

1. Standard OAuth 2.0 authorization code flow
2. Secure token storage in the SecureStorage table
3. Automatic token refresh when expired
4. Local HTTP listener for authorization code callback
5. Proper scope limitations for Calendar access

Implementation approach:

```csharp
public class GoogleOAuthService : IOAuthService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    // Constructor and fields...
    
    public async Task<OAuthToken> AuthenticateAsync()
    {
        // Generate authorization URL with appropriate scopes
        var authUrl = GenerateAuthorizationUrl();
        
        // Open browser for user authentication
        Process.Start(new ProcessStartInfo
        {
            FileName = authUrl,
            UseShellExecute = true
        });
        
        // Start local listener for authorization code
        var authCode = await ListenForAuthorizationCodeAsync();
        
        // Exchange code for tokens
        var tokens = await ExchangeCodeForTokensAsync(authCode);
        
        // Store tokens securely
        await _secureStorage.StoreSecureValueAsync("google_access_token", tokens.AccessToken);
        await _secureStorage.StoreSecureValueAsync("google_refresh_token", tokens.RefreshToken);
        await _secureStorage.StoreSecureValueAsync("google_token_expiry", tokens.ExpiryTime.ToString());
        
        return tokens;
    }
    
    public async Task<OAuthToken> GetValidTokenAsync()
    {
        var accessToken = await _secureStorage.RetrieveSecureValueAsync("google_access_token");
        var refreshToken = await _secureStorage.RetrieveSecureValueAsync("google_refresh_token");
        var expiryTimeStr = await _secureStorage.RetrieveSecureValueAsync("google_token_expiry");
        
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            return await AuthenticateAsync();
            
        if (DateTime.TryParse(expiryTimeStr, out var expiryTime) && expiryTime <= DateTime.UtcNow.AddMinutes(5))
            return await RefreshTokenAsync(refreshToken);
            
        return new OAuthToken
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiryTime = expiryTime
        };
    }
    
    // Other methods...
}
```

### 5.3 Data Protection

To protect sensitive user and student data:

1. All student information will be stored in the local database only
2. No unnecessary student data will be transmitted to external services
3. Only anonymized or aggregated data will be used in LLM contexts when appropriate
4. Database file will use SQLite encryption extension if available
5. File system access will be limited to the designated scratchpad folder

### 5.4 Communication Security

All external communications will implement:

1. TLS 1.2+ for all HTTP communications
2. WSS (secure WebSockets) for fish.audio integration
3. Certificate validation for all secure connections
4. Proper error handling for security-related failures
5. No sensitive data transmission in query parameters

## 6. Error Handling and Recovery

### 6.1 Error Handling Strategy

The application will implement a layered error handling approach:

1. **Local error handling**: Try/catch blocks at operation level
2. **Service error handling**: Error transformation and logging
3. **Global error handling**: Application-wide handler for uncaught exceptions
4. **User feedback**: Appropriate error messages based on context

Error classification:

| Severity | Description | Handling |
|----------|-------------|----------|
| Critical | Application cannot continue | Visual notification, logging, graceful shutdown if necessary |
| Error | Feature/component failure | Visual notification, logging, feature disablement |
| Warning | Operation issue, can continue | Visual notification, logging, retry with backoff |
| Info | Non-critical issue | Logging only, silent retry |

### 6.2 Recovery Mechanisms

The application will implement the following recovery approaches:

1. **Service Dependencies**:
   - Automatic reconnection for network services
   - Circuit breaker pattern for failing services
   - Fallback providers when primary providers fail

2. **Data Corruption**:
   - Database transaction rollback on error
   - Automatic database backup before schema changes
   - Database integrity check on startup

3. **Application State**:
   - Periodic state saving to survive crashes
   - Settings backup before configuration changes
   - Last known good configuration recovery

### 6.3 Logging Strategy

The application will implement comprehensive logging using Serilog:

1. **Log Levels**:
   - Verbose: Detailed debugging information
   - Debug: Standard debugging information
   - Information: General operational events
   - Warning: Non-critical issues
   - Error: Functional failures
   - Fatal: Application crashes

2. **Log Targets**:
   - File logging with daily rotation
   - In-memory buffer for UI display
   - Windows event log for critical errors

3. **Log Enrichment**:
   - Component/module identification
   - Operation correlation IDs
   - Timing information
   - User action context

Example logging configuration:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.File(
        path: "logs/adept-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {NewLine}{Exception}")
    .WriteTo.Memory(new MemorySink(), outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}")
    .CreateLogger();
```
## 7. Integration Design

### 7.1 STT Integration

The application will integrate with multiple Speech-to-Text providers through a provider-agnostic interface:

```csharp
public interface ISpeechToTextProvider
{
    Task<string> TranscribeAsync(byte[] audioData, SttOptions options = null);
    bool SupportsStreamingRecognition { get; }
    Task<IStreamingRecognitionSession> StartStreamingSessionAsync(SttOptions options = null);
}
```

#### 7.1.1 Google STT Implementation

```csharp
public class GoogleSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    
    public GoogleSpeechToTextProvider(ISecureStorageService secureStorage, IHttpClientFactory httpClientFactory)
    {
        _apiKey = await secureStorage.RetrieveSecureValueAsync("google_api_key");
        _httpClient = httpClientFactory.CreateClient("GoogleSTT");
        _httpClient.BaseAddress = new Uri("https://speech.googleapis.com/v1/");
    }
    
    public bool SupportsStreamingRecognition => true;
    
    public async Task<string> TranscribeAsync(byte[] audioData, SttOptions options = null)
    {
        var request = new
        {
            audio = new { content = Convert.ToBase64String(audioData) },
            config = new
            {
                languageCode = options?.LanguageCode ?? "en-GB",
                model = "command_and_search",
                useEnhanced = true,
                enableAutomaticPunctuation = true
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            $"speech:recognize?key={_apiKey}", 
            request);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<GoogleSttResponse>();
        
        return result?.Results
            ?.SelectMany(r => r.Alternatives)
            ?.OrderByDescending(a => a.Confidence)
            ?.FirstOrDefault()
            ?.Transcript ?? string.Empty;
    }
    
    public async Task<IStreamingRecognitionSession> StartStreamingSessionAsync(SttOptions options = null)
    {
        return new GoogleStreamingRecognitionSession(_apiKey, options);
    }
}
```

#### 7.1.2 OpenAI Whisper Implementation

```csharp
public class WhisperSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    
    public WhisperSpeechToTextProvider(ISecureStorageService secureStorage, IHttpClientFactory httpClientFactory)
    {
        _apiKey = await secureStorage.RetrieveSecureValueAsync("openai_api_key");
        _httpClient = httpClientFactory.CreateClient("OpenAIWhisper");
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }
    
    public bool SupportsStreamingRecognition => false;
    
    public async Task<string> TranscribeAsync(byte[] audioData, SttOptions options = null)
    {
        // Convert audio to proper format if needed
        var audioContent = new ByteArrayContent(audioData);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        
        var formData = new MultipartFormDataContent
        {
            { audioContent, "file", "audio.wav" },
            { new StringContent(options?.LanguageCode?.Split('-')[0] ?? "en"), "language" },
            { new StringContent("transcriptions"), "model" }
        };
        
        var response = await _httpClient.PostAsync("audio/transcriptions", formData);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<WhisperResponse>();
        
        return result?.Text ?? string.Empty;
    }
    
    public Task<IStreamingRecognitionSession> StartStreamingSessionAsync(SttOptions options = null)
    {
        throw new NotSupportedException("Whisper does not support streaming recognition");
    }
}
```

#### 7.1.3 STT Factory and Selection

```csharp
public class SpeechToTextFactory : ISpeechToTextFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configService;
    
    public SpeechToTextFactory(IServiceProvider serviceProvider, IConfigurationService configService)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
    }
    
    public async Task<ISpeechToTextProvider> CreateProviderAsync()
    {
        var providerName = await _configService.GetConfigurationValueAsync("stt_provider", "google");
        
        return providerName.ToLowerInvariant() switch
        {
            "google" => _serviceProvider.GetRequiredService<GoogleSpeechToTextProvider>(),
            "whisper" => _serviceProvider.GetRequiredService<WhisperSpeechToTextProvider>(),
            _ => throw new ArgumentException($"Unsupported STT provider: {providerName}")
        };
    }
}
```

### 7.2 LLM Integration

The application will integrate with multiple LLM providers through a unified interface:

```csharp
public interface ILlmProvider
{
    Task<string> CompleteAsync(string prompt, LlmOptions options = null);
    Task<string> ChatAsync(IEnumerable<ChatMessage> messages, LlmOptions options = null);
    bool SupportsStreaming { get; }
    Task StreamChatAsync(IEnumerable<ChatMessage> messages, Action<string> onToken, LlmOptions options = null);
    Task<IEnumerable<LlmTool>> GetSupportedToolsAsync();
    Task<LlmToolResult> ExecuteToolAsync(string toolName, string toolInput);
}
```

#### 7.2.1 Provider-Specific Implementations

The application will implement specific adapters for:
- OpenAI
- Anthropic
- Openrouter
- Deepseek
- Meta
- Google

Example for Anthropic implementation:

```csharp
public class AnthropicProvider : ILlmProvider
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly IToolManager _toolManager;
    
    public AnthropicProvider(
        ISecureStorageService secureStorage, 
        IHttpClientFactory httpClientFactory,
        IToolManager toolManager)
    {
        _apiKey = await secureStorage.RetrieveSecureValueAsync("anthropic_api_key");
        _httpClient = httpClientFactory.CreateClient("Anthropic");
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _toolManager = toolManager;
    }
    
    public bool SupportsStreaming => true;
    
    public async Task<string> CompleteAsync(string prompt, LlmOptions options = null)
    {
        var messages = new[] { new ChatMessage { Role = "user", Content = prompt } };
        return await ChatAsync(messages, options);
    }
    
    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages, LlmOptions options = null)
    {
        var request = new
        {
            model = options?.Model ?? "claude-3-opus-20240229",
            max_tokens = options?.MaxTokens ?? 1000,
            temperature = options?.Temperature ?? 0.7,
            messages = messages.Select(m => new 
            {
                role = m.Role == "assistant" ? "assistant" : "user",
                content = m.Content
            }),
            tools = (options?.UseTools ?? false) ? await GetToolsArrayAsync() : null
        };
        
        var response = await _httpClient.PostAsJsonAsync("messages", request);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>();
        
        // Handle tool calls if present
        if (result?.Content?.FirstOrDefault()?.Type == "tool_use")
        {
            var toolCall = result.Content.First();
            var toolResult = await ExecuteToolAsync(toolCall.ToolName, toolCall.ToolInput);
            
            // Continue conversation with tool result
            var updatedMessages = messages.ToList();
            updatedMessages.Add(new ChatMessage { Role = "assistant", Content = $"<tool_use>{toolCall.ToolName}</tool_use>" });
            updatedMessages.Add(new ChatMessage { Role = "user", Content = $"<tool_result>{toolResult.Result}</tool_result>" });
            
            return await ChatAsync(updatedMessages, options);
        }
        
        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }
    
    // Implement other interface methods...
    
    private async Task<object> GetToolsArrayAsync()
    {
        var tools = await GetSupportedToolsAsync();
        
        return tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.InputSchema
        }).ToArray();
    }
}
```

#### 7.2.2 LLM Context Management

```csharp
public class LlmContextManager : ILlmContextManager
{
    private readonly IClassRepository _classRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ISystemPromptRepository _promptRepository;
    
    // Constructor and dependencies...
    
    public async Task<IEnumerable<ChatMessage>> BuildContextAsync(
        string userMessage, 
        string classId = null, 
        string lessonId = null,
        string systemPromptId = null)
    {
        var messages = new List<ChatMessage>();
        
        // Add system prompt
        var systemPrompt = systemPromptId != null 
            ? await _promptRepository.GetByIdAsync(systemPromptId)
            : await _promptRepository.GetDefaultPromptAsync();
            
        if (systemPrompt != null)
        {
            messages.Add(new ChatMessage
            {
                Role = "system",
                Content = await EnrichSystemPromptAsync(systemPrompt.Content, classId, lessonId)
            });
        }
        
        // Add conversation history if available
        if (classId != null)
        {
            var recentHistory = await _conversationRepository.GetRecentHistoryAsync(classId);
            messages.AddRange(recentHistory.Select(h => new ChatMessage
            {
                Role = h.IsUser ? "user" : "assistant",
                Content = h.Message
            }));
        }
        
        // Add current user message
        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = userMessage
        });
        
        return messages;
    }
    
    private async Task<string> EnrichSystemPromptAsync(string promptTemplate, string classId, string lessonId)
    {
        // If no class or lesson context, return the basic prompt
        if (classId == null && lessonId == null)
            return promptTemplate;
            
        // Replace template variables with actual data
        var enrichedPrompt = promptTemplate;
        
        if (classId != null)
        {
            var classInfo = await _classRepository.GetClassByIdAsync(classId);
            if (classInfo != null)
            {
                enrichedPrompt = enrichedPrompt.Replace("{{CLASS_CODE}}", classInfo.ClassCode);
                enrichedPrompt = enrichedPrompt.Replace("{{EDUCATION_LEVEL}}", classInfo.EducationLevel);
                enrichedPrompt = enrichedPrompt.Replace("{{CURRENT_TOPIC}}", classInfo.CurrentTopic ?? "");
                
                // Add student information if available
                var students = await _classRepository.GetStudentsForClassAsync(classId);
                if (students.Any())
                {
                    var studentInfo = string.Join("\n", students.Select(s => 
                        $"- {s.Name}: FSM={s.FsmStatus}, SEN={s.SenStatus}, EAL={s.EalStatus}, " +
                        $"Ability={s.AbilityLevel}, ReadingAge={s.ReadingAge}, TargetGrade={s.TargetGrade}"));
                    
                    enrichedPrompt = enrichedPrompt.Replace("{{STUDENTS}}", studentInfo);
                }
                else
                {
                    enrichedPrompt = enrichedPrompt.Replace("{{STUDENTS}}", "No student information available.");
                }
            }
        }
        
        if (lessonId != null)
        {
            var lessonInfo = await _lessonRepository.GetLessonByIdAsync(lessonId);
            if (lessonInfo != null)
            {
                enrichedPrompt = enrichedPrompt.Replace("{{LESSON_TITLE}}", lessonInfo.Title);
                enrichedPrompt = enrichedPrompt.Replace("{{LEARNING_OBJECTIVES}}", lessonInfo.LearningObjectives ?? "");
                
                // Add lesson components if available
                var components = lessonInfo.Components;
                if (components != null)
                {
                    enrichedPrompt = enrichedPrompt.Replace("{{RETRIEVAL_QUESTIONS}}", FormatRetrievalQuestions(components));
                    enrichedPrompt = enrichedPrompt.Replace("{{CHALLENGE_QUESTION}}", components.ChallengeQuestion?.Question ?? "");
                    enrichedPrompt = enrichedPrompt.Replace("{{BIG_QUESTION}}", components.BigQuestion ?? "");
                    // Add other components...
                }
            }
        }
        
        return enrichedPrompt;
    }
    
    private string FormatRetrievalQuestions(LessonComponents components)
    {
        if (components.RetrievalQuestions == null || !components.RetrievalQuestions.Any())
            return "No retrieval questions available.";
            
        return string.Join("\n", components.RetrievalQuestions.Select((q, i) => 
            $"{i+1}. Question: {q.Question}\n   Answer: {q.Answer}"));
    }
}
```

### 7.3 TTS Integration with fish.audio

The application will integrate with fish.audio via WebSocket for Text-to-Speech:

```csharp
public class FishAudioTtsProvider : ITtsProvider
{
    private readonly string _apiKey;
    private readonly string _customVoiceId;
    private readonly ILogger<FishAudioTtsProvider> _logger;
    
    public FishAudioTtsProvider(
        ISecureStorageService secureStorage,
        IConfigurationService configService,
        ILogger<FishAudioTtsProvider> logger)
    {
        _apiKey = await secureStorage.RetrieveSecureValueAsync("fish_audio_api_key");
        _customVoiceId = await configService.GetConfigurationValueAsync("fish_audio_voice_id");
        _logger = logger;
    }
    
    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        using var audioOutput = new WaveOutEvent();
        var tcsCompletion = new TaskCompletionSource<bool>();
        
        try
        {
            var wsClient = new ClientWebSocket();
            wsClient.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
            
            await wsClient.ConnectAsync(new Uri("wss://api.fish.audio/v1/tts/ws"), cancellationToken);
            
            // Send TTS request
            var request = new
            {
                text = text,
                voice_id = _customVoiceId,
                stream = true,
                format = "wav"
            };
            
            var requestJson = JsonSerializer.Serialize(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            await wsClient.SendAsync(
                new ArraySegment<byte>(requestBytes), 
                WebSocketMessageType.Text, 
                true, 
                cancellationToken);
                
            // Process streaming audio response
            using var memoryStream = new MemoryStream();
            var buffer = new byte[8192];
            var isFirstChunk = true;
            var waveProvider = new BufferedWaveProvider(new WaveFormat(24000, 16, 1));
            
            audioOutput.Init(waveProvider);
            audioOutput.Play();
            
            while (wsClient.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await wsClient.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    cancellationToken);
                    
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await wsClient.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Completed", 
                        cancellationToken);
                    break;
                }
                
                // Skip WAV header for all chunks except the first
                var audioData = new ArraySegment<byte>(
                    buffer, 
                    isFirstChunk ? 0 : 44, 
                    isFirstChunk ? result.Count : result.Count - 44);
                    
                waveProvider.AddSamples(audioData.Array, audioData.Offset, audioData.Count);
                isFirstChunk = false;
            }
            
            // Wait for audio to finish playing
            while (waveProvider.BufferedBytes > 0)
            {
                await Task.Delay(100, cancellationToken);
            }
            
            await Task.Delay(300, cancellationToken); // Short delay after speech ends
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fish.audio TTS processing");
            throw;
        }
        finally
        {
            audioOutput.Stop();
            audioOutput.Dispose();
        }
    }
    
    public async Task<byte[]> SynthesizeToWavAsync(string text, CancellationToken cancellationToken = default)
    {
        // Implementation for non-streaming synthesis
        // Similar to above but accumulates all audio data before returning
    }
}
```

### 7.4 MCP Server Integration

The application will integrate with MCP servers using a standardized interface:

```csharp
public interface IMcpToolProvider
{
    string ToolName { get; }
    string Description { get; }
    object InputSchema { get; }
    Task<object> ExecuteAsync(string input);
    Task InitializeAsync();
    bool IsInitialized { get; }
}
```

#### 7.4.1 MCP Server Manager

```csharp
public class McpServerManager : IMcpServerManager
{
    private readonly ILogger<McpServerManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDictionary<string, IMcpToolProvider> _toolProviders;
    private readonly IDictionary<string, Process> _serverProcesses;
    
    public McpServerManager(
        IEnumerable<IMcpToolProvider> toolProviders,
        ILogger<McpServerManager> logger,
        IConfiguration configuration)
    {
        _toolProviders = toolProviders.ToDictionary(p => p.ToolName);
        _serverProcesses = new Dictionary<string, Process>();
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task InitializeAllServersAsync()
    {
        foreach (var provider in _toolProviders.Values)
        {
            try
            {
                await InitializeServerAsync(provider.ToolName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP server: {ServerName}", provider.ToolName);
            }
        }
    }
    
    public async Task InitializeServerAsync(string toolName)
    {
        if (!_toolProviders.TryGetValue(toolName, out var provider))
        {
            throw new ArgumentException($"Unknown tool provider: {toolName}");
        }
        
        if (provider.IsInitialized)
        {
            return; // Already initialized
        }
        
        // Start the MCP server process if needed
        var serverConfig = _configuration.GetSection($"McpServers:{toolName}");
        var serverExecutable = serverConfig["Executable"];
        
        if (!string.IsNullOrEmpty(serverExecutable))
        {
            var serverArgs = serverConfig["Arguments"] ?? "";
            var serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppContext.BaseDirectory, "lib", "mcp", serverExecutable),
                    Arguments = serverArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            
            _logger.LogInformation("Starting MCP server: {ServerName}", toolName);
            
            serverProcess.OutputDataReceived += (sender, args) => 
                _logger.LogDebug("MCP server {ServerName} output: {Output}", toolName, args.Data);
                
            serverProcess.ErrorDataReceived += (sender, args) => 
                _logger.LogWarning("MCP server {ServerName} error: {Error}", toolName, args.Data);
                
            serverProcess.Start();
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
            
            _serverProcesses[toolName] = serverProcess;
            
            // Wait for server to start up
            await Task.Delay(serverConfig.GetValue("StartupDelayMs", 1000));
        }
        
        // Initialize the tool provider
        await provider.InitializeAsync();
    }
    
    public async Task<object> ExecuteToolAsync(string toolName, string input)
    {
        if (!_toolProviders.TryGetValue(toolName, out var provider))
        {
            throw new ArgumentException($"Unknown tool provider: {toolName}");
        }
        
        if (!provider.IsInitialized)
        {
            await InitializeServerAsync(toolName);
        }
        
        return await provider.ExecuteAsync(input);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var process in _serverProcesses.Values)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    await process.WaitForExitAsync();
                }
                
                process.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MCP server process");
            }
        }
        
        _serverProcesses.Clear();
    }
}
```

#### 7.4.2 Example MCP Tool Provider: File System

```csharp
public class FileSystemToolProvider : IMcpToolProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileSystemToolProvider> _logger;
    private readonly IConfigurationService _configService;
    private bool _isInitialized;
    
    public string ToolName => "filesystem";
    public string Description => "Access to read and write files in the designated scratchpad folder";
    public bool IsInitialized => _isInitialized;
    
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            operation = new
            {
                type = "string",
                enumValues = new[] { "read", "write", "list", "delete" }
            },
            path = new
            {
                type = "string",
                description = "File or directory path relative to the scratchpad folder"
            },
            content = new
            {
                type = "string",
                description = "Content to write (for write operation)"
            }
        },
        required = new[] { "operation", "path" }
    };
    
    public FileSystemToolProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<FileSystemToolProvider> logger,
        IConfigurationService configService)
    {
        _httpClient = httpClientFactory.CreateClient("McpServers");
        _logger = logger;
        _configService = configService;
    }
    
    public async Task InitializeAsync()
    {
        try
        {
            // Set up the scratchpad folder path
            var scratchpadPath = await _configService.GetConfigurationValueAsync(
                "scratchpad_folder", 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Adept", "Scratchpad"));
                
            // Ensure directory exists
            Directory.CreateDirectory(scratchpadPath);
            
            // Configure the MCP server
            var configRequest = new
            {
                base_path = scratchpadPath,
                allow_subfolders = true
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:5010/filesystem/configure", 
                configRequest);
                
            response.EnsureSuccessStatusCode();
            
            _isInitialized = true;
            _logger.LogInformation("Filesystem MCP server initialized with scratchpad path: {Path}", scratchpadPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Filesystem MCP server");
            throw;
        }
    }
    
    public async Task<object> ExecuteAsync(string input)
    {
        try
        {
            var request = JsonSerializer.Deserialize<JsonElement>(input);
            var operation = request.GetProperty("operation").GetString();
            
            var endpoint = operation switch
            {
                "read" => "read",
                "write" => "write",
                "list" => "list",
                "delete" => "delete",
                _ => throw new ArgumentException($"Unsupported filesystem operation: {operation}")
            };
            
            var response = await _httpClient.PostAsync(
                $"http://localhost:5010/filesystem/{endpoint}",
                new StringContent(input, Encoding.UTF8, "application/json"));
                
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing filesystem MCP operation");
            throw;
        }
    }
}
```

### 7.5 Google Calendar Integration

The application will integrate with Google Calendar via the MCP server:

```csharp
public class GoogleCalendarService : ICalendarService
{
    private readonly IMcpServerManager _mcpManager;
    private readonly ILogger<GoogleCalendarService> _logger;
    
    public GoogleCalendarService(
        IMcpServerManager mcpManager,
        ILogger<GoogleCalendarService> logger)
    {
        _mcpManager = mcpManager;
        _logger = logger;
    }
    
    public async Task InitializeAsync()
    {
        await _mcpManager.InitializeServerAsync("google_calendar");
    }
    
    public async Task<string> CreateLessonEventAsync(LessonPlan lesson, Class classInfo)
    {
        try
        {
            var startTime = TimeOnly.Parse(GetTimeSlotStart(lesson.TimeSlot))
                .ToTimeSpan();
                
            var endTime = startTime.Add(TimeSpan.FromHours(1));
            
            var startDateTime = DateOnly.Parse(lesson.Date)
                .ToDateTime(startTime);
                
            var endDateTime = DateOnly.Parse(lesson.Date)
                .ToDateTime(endTime);
                
            var description = BuildEventDescription(lesson);
            
            var request = new
            {
                operation = "create",
                event_details = new
                {
                    summary = $"{classInfo.ClassCode}: {lesson.Title}",
                    description = description,
                    location = "Classroom",
                    start = new
                    {
                        dateTime = startDateTime.ToString("o"),
                        timeZone = "Europe/London"
                    },
                    end = new
                    {
                        dateTime = endDateTime.ToString("o"),
                        timeZone = "Europe/London"
                    }
                }
            };
            
            var result = await _mcpManager.ExecuteToolAsync(
                "google_calendar", 
                JsonSerializer.Serialize(request));
                
            var eventId = ((JsonElement)result).GetProperty("event_id").GetString();
            
            _logger.LogInformation("Created calendar event for lesson: {LessonTitle}", lesson.Title);
            
            return eventId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event for lesson: {LessonTitle}", lesson.Title);
            throw;
        }
    }
    
    public async Task UpdateLessonEventAsync(string eventId, LessonPlan lesson, Class classInfo)
    {
        // Similar to create but with "update" operation and eventId
    }
    
    public async Task<IEnumerable<CalendarEvent>> GetEventsForDateAsync(string date)
    {
        try
        {
            var request = new
            {
                operation = "list",
                time_min = $"{date}T00:00:00Z",
                time_max = $"{date}T23:59:59Z"
            };
            
            var result = await _mcpManager.ExecuteToolAsync(
                "google_calendar", 
                JsonSerializer.Serialize(request));
                
            var events = ((JsonElement)result).GetProperty("events");
            
            return JsonSerializer.Deserialize<IEnumerable<CalendarEvent>>(events.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve calendar events for date: {Date}", date);
            throw;
        }
    }
    
    private string BuildEventDescription(LessonPlan lesson)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Learning Objectives");
        sb.AppendLine(lesson.LearningObjectives);
        sb.AppendLine();
        
        sb.AppendLine("# Retrieval Questions");
        foreach (var question in lesson.Components.RetrievalQuestions)
        {
            sb.AppendLine($"- Q: {question.Question}");
            sb.AppendLine($"  A: {question.Answer}");
        }
        sb.AppendLine();
        
        sb.AppendLine("# Challenge Question");
        sb.AppendLine($"Q: {lesson.Components.ChallengeQuestion.Question}");
        sb.AppendLine($"A: {lesson.Components.ChallengeQuestion.Answer}");
        sb.AppendLine();
        
        sb.AppendLine("# Big Question");
        sb.AppendLine(lesson.Components.BigQuestion);
        sb.AppendLine();
        
        sb.AppendLine("# Starter Activity");
        sb.AppendLine(lesson.Components.StarterActivity);
        sb.AppendLine();
        
        sb.AppendLine("# Main Activity");
        sb.AppendLine(lesson.Components.MainActivity);
        sb.AppendLine();
        
        sb.AppendLine("# Plenary Activity");
        sb.AppendLine(lesson.Components.PlenaryActivity);
        
        return sb.ToString();
    }
    
    private string GetTimeSlotStart(int timeSlot)
    {
        return timeSlot switch
        {
            0 => "09:00",
            1 => "10:00",
            2 => "11:30",
            3 => "12:30",
            4 => "14:00",
            _ => throw new ArgumentException($"Invalid time slot: {timeSlot}")
        };
    }
}
```

## 8. User Interface Implementation

### 8.1 User Interface Technology

The application will use Windows Presentation Foundation (WPF) with:
- MVVM pattern for data binding
- Modern UI controls
- Responsive layout using Grid and StackPanel
- ResourceDictionaries for consistent styling
- Dependency properties for data binding

### 8.2 Main Window Implementation

```xml
<!-- MainWindow.xaml -->
<Window x:Class="Adept.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:Adept.UI"
        xmlns:viewModels="clr-namespace:Adept.UI.ViewModels"
        xmlns:views="clr-namespace:Adept.UI.Views"
        mc:Ignorable="d"
        Title="Adept AI Teaching Assistant"
        MinHeight="600" MinWidth="800"
        Height="720" Width="1024">
    
    <Window.Resources>
        <ResourceDictionary Source="/Adept.UI;component/Resources/Styles.xaml" />
    </Window.Resources>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        
        <!-- Main Tab Control -->
        <TabControl Grid.Row="1" 
                    SelectedIndex="{Binding SelectedTabIndex}" 
                    Style="{StaticResource MainTabControlStyle}">
            
            <TabItem Header="Home">
                <views:HomeView DataContext="{Binding HomeViewModel}" />
            </TabItem>
            
            <TabItem Header="Classes">
                <views:ClassesView DataContext="{Binding ClassesViewModel}" />
            </TabItem>
            
            <TabItem Header="Lesson Planner">
                <views:LessonPlannerView DataContext="{Binding LessonPlannerViewModel}" />
            </TabItem>
            
            <TabItem Header="Configuration">
                <views:ConfigurationView DataContext="{Binding ConfigurationViewModel}" />
            </TabItem>
            
            <TabItem Header="System Status">
                <views:StatusView DataContext="{Binding StatusViewModel}" />
            </TabItem>
        </TabControl>
        
        <!-- Status Bar -->
        <StatusBar Grid.Row="2" Height="28">
            <StatusBar.ItemsPanel>
                <ItemsPanelTemplate>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>
                    </Grid>
                </ItemsPanelTemplate>
            </StatusBar.ItemsPanel>
            
            <StatusBarItem Grid.Column="0">
                <StackPanel Orientation="Horizontal">
                    <Ellipse Width="12" Height="12" Margin="5,0" 
                             Fill="{Binding IsListening, Converter={StaticResource BooleanToColorConverter}}" />
                    <TextBlock Text="{Binding ListeningStatus}" />
                </StackPanel>
            </StatusBarItem>
            
            <StatusBarItem Grid.Column="2">
                <TextBlock>
                    <Run Text="Current Lesson: " />
                    <Run Text="{Binding CurrentLessonInfo, Mode=OneWay}" FontWeight="SemiBold" />
                </TextBlock>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

### 8.3 Configuration View Implementation

```xml
<!-- ConfigurationView.xaml -->
<UserControl x:Class="Adept.UI.Views.ConfigurationView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:Adept.UI.Views"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
    
    <Grid Margin="15">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        
        <TabControl Grid.Row="1">
            <!-- Speech Recognition Tab -->
            <TabItem Header="Speech Recognition">
                <Grid Margin="10">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="Provider:" VerticalAlignment="Center" />
                    <ComboBox Grid.Row="0" Grid.Column="1" Margin="5"
                              ItemsSource="{Binding SttProviders}"
                              SelectedItem="{Binding SelectedSttProvider}"
                              DisplayMemberPath="Name" />
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="API Key:" VerticalAlignment="Center" />
                    <PasswordBox Grid.Row="1" Grid.Column="1" Margin="5"
                                 local:PasswordBoxHelper.Password="{Binding SttApiKey, Mode=TwoWay}" />
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Language:" VerticalAlignment="Center" />
                    <ComboBox Grid.Row="2" Grid.Column="1" Margin="5"
                              ItemsSource="{Binding LanguageOptions}"
                              SelectedItem="{Binding SelectedLanguage}"
                              DisplayMemberPath="Name" />
                    
                    <Button Grid.Row="3" Grid.Column="1" Content="Test Speech Recognition"
                            Command="{Binding TestSttCommand}"
                            HorizontalAlignment="Left"
                            Margin="5,10" Padding="10,5" />
                </Grid>
            </TabItem>
            
            <!-- LLM Tab -->
            <TabItem Header="AI Models">
                <!-- Similar structure to STT tab but with LLM options -->
            </TabItem>
            
            <!-- Text-to-Speech Tab -->
            <TabItem Header="Text-to-Speech">
                <!-- Structure for TTS configuration -->
            </TabItem>
            
            <!-- System Prompts Tab -->
            <TabItem Header="System Prompts">
                <!-- Structure for managing system prompts -->
            </TabItem>
            
            <!-- Folders Tab -->
            <TabItem Header="Folders">
                <!-- Structure for configuring scratchpad folder -->
            </TabItem>
            
            <!-- MCP Servers Tab -->
            <TabItem Header="MCP Servers">
                <!-- Structure for configuring MCP servers -->
            </TabItem>
        </TabControl>
    </Grid>
</UserControl>
```

### 8.4 Lesson Planner View Implementation

```xml
<!-- LessonPlannerView.xaml -->
<UserControl x:Class="Adept.UI.Views.LessonPlannerView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:Adept.UI.Views"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
    
    <Grid Margin="15">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="250" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        
        <!-- Calendar and Class Selection -->
        <Grid Grid.Column="0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            
            <TextBlock Grid.Row="0" Text="Class:" Margin="0,0,0,5" />
            <ComboBox Grid.Row="1" 
                      ItemsSource="{Binding Classes}"
                      SelectedItem="{Binding SelectedClass}"
                      DisplayMemberPath="ClassCode"
                      Margin="0,0,0,10" />
            
            <Calendar Grid.Row="2"
                      SelectedDate="{Binding SelectedDate}"
                      DisplayDate="{Binding SelectedDate}" />
            
            <Button Grid.Row="3" Content="Create New Lesson"
                    Command="{Binding CreateLessonCommand}"
                    Margin="0,10,0,0" Padding="10,5" />
        </Grid>
        
        <!-- Lesson Details Editor -->
        <Grid Grid.Column="1" Margin="15,0,0,0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            
            <!-- Lesson Basic Info -->
            <StackPanel Grid.Row="0">
                <TextBlock Text="{Binding LessonHeaderText}" FontSize="18" FontWeight="SemiBold" Margin="0,0,0,10" />
                
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="150" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="100" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
                    
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="Lesson Title:" VerticalAlignment="Center" />
                    <TextBox Grid.Row="0" Grid.Column="1" Grid.ColumnSpan="3"
                             Text="{Binding CurrentLesson.Title, UpdateSourceTrigger=PropertyChanged}"
                             Margin="0,5" />
                    
                    <TextBlock Grid.Row="1" Grid.Column="0" Text="Date:" VerticalAlignment="Center" />
                    <DatePicker Grid.Row="1" Grid.Column="1"
                                SelectedDate="{Binding LessonDate}"
                                Margin="0,5" />
                    
                    <TextBlock Grid.Row="1" Grid.Column="2" Text="Time Slot:" VerticalAlignment="Center" />
                    <ComboBox Grid.Row="1" Grid.Column="3"
                              ItemsSource="{Binding TimeSlots}"
                              SelectedIndex="{Binding SelectedTimeSlotIndex}"
                              Margin="0,5" />
                    
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="Learning Objectives:" VerticalAlignment="Top" Margin="0,5,0,0" />
                    <TextBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="3"
                             Text="{Binding CurrentLesson.LearningObjectives, UpdateSourceTrigger=PropertyChanged}"
                             TextWrapping="Wrap"
                             AcceptsReturn="True"
                             Height="60"
                             Margin="0,5" />
                </Grid>
            </StackPanel>
            
            <!-- Lesson Components Tabs -->
            <TabControl Grid.Row="1" Margin="0,10,0,0">
                <TabItem Header="Retrieval Questions">
                    <!-- Retrieval questions editor -->
                </TabItem>
                
                <TabItem Header="Challenge Question">
                    <!-- Challenge question editor -->
                </TabItem>
                
                <TabItem Header="Big Question">
                    <!-- Big question editor -->
                </TabItem>
                
                <TabItem Header="Activities">
                    <!-- Activities editor -->
                </TabItem>
            </TabControl>
            
            <!-- Action Buttons -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
                <Button Content="Generate Suggestions" Command="{Binding GenerateSuggestionsCommand}" Padding="10,5" Margin="5,0" />
                <Button Content="Save Lesson" Command="{Binding SaveLessonCommand}" Padding="10,5" Margin="5,0" />
                <Button Content="Sync with Calendar" Command="{Binding SyncWithCalendarCommand}" Padding="10,5" Margin="5,0" />
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```
## 9. Wake Word Detection Implementation

### 9.1 Wake Word Detection Approach

The application will implement a two-stage wake word detection system for optimal performance:

1. **Always-On Lightweight Detection:**
   - Continuously processes audio input with minimal CPU usage
   - Uses a lightweight algorithm optimized for the specific wake word "Adept"
   - Triggers second-stage detection when a potential match is found

2. **Confirmation Detection:**
   - Uses more sophisticated audio processing to confirm the wake word
   - Eliminates false positives from the first stage
   - Transitions to full STT processing when confirmed

This approach balances responsiveness with resource efficiency and accuracy.

### 9.2 Implementation Details

```csharp
public class WakeWordDetector : IWakeWordDetector, IDisposable
{
    private readonly WaveInEvent _waveIn;
    private readonly ILogger<WakeWordDetector> _logger;
    private readonly MemoryStream _audioBuffer;
    private readonly IWaveProvider _waveProvider;
    private readonly IWakeWordModel _wakeWordModel;
    private readonly SemaphoreSlim _processingLock = new SemaphoreSlim(1, 1);
    private readonly ConcurrentQueue<byte[]> _audioChunks = new ConcurrentQueue<byte[]>();
    private readonly CancellationTokenSource _processingCts = new CancellationTokenSource();
    
    private Task _processingTask;
    private bool _isDisposed;
    
    public event EventHandler<WakeWordDetectedEventArgs> WakeWordDetected;
    
    public bool IsListening { get; private set; }
    
    public WakeWordDetector(
        IWakeWordModel wakeWordModel,
        ILogger<WakeWordDetector> logger,
        IConfigurationService configService)
    {
        _wakeWordModel = wakeWordModel;
        _logger = logger;
        
        // Initialize audio capture
        _waveIn = new WaveInEvent
        {
            DeviceNumber = -1, // Default device
            WaveFormat = new WaveFormat(16000, 1), // 16kHz mono for speech recognition
            BufferMilliseconds = 50 // Small buffer for responsiveness
        };
        
        _audioBuffer = new MemoryStream();
        _waveProvider = new BufferedWaveProvider(_waveIn.WaveFormat);
        
        _waveIn.DataAvailable += WaveIn_DataAvailable;
        
        // Start background processing
        _processingTask = Task.Run(ProcessAudioChunksAsync, _processingCts.Token);
    }
    
    public void Start()
    {
        if (!IsListening)
        {
            _waveIn.StartRecording();
            IsListening = true;
            _logger.LogInformation("Wake word detector started");
        }
    }
    
    public void Stop()
    {
        if (IsListening)
        {
            _waveIn.StopRecording();
            IsListening = false;
            _logger.LogInformation("Wake word detector stopped");
        }
    }
    
    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        // Copy audio data to avoid race conditions
        var buffer = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, buffer, e.BytesRecorded);
        
        // Add to processing queue
        _audioChunks.Enqueue(buffer);
    }
    
    private async Task ProcessAudioChunksAsync()
    {
        try
        {
            while (!_processingCts.Token.IsCancellationRequested)
            {
                if (_audioChunks.TryDequeue(out var chunk))
                {
                    await _processingLock.WaitAsync();
                    
                    try
                    {
                        // Stage 1: Lightweight detection (energy-based and simple pattern matching)
                        if (IsPotentialWakeWord(chunk))
                        {
                            // Add the audio to buffer for confirmation
                            _audioBuffer.Write(chunk, 0, chunk.Length);
                            
                            // If we have enough audio for confirmation (1.5 seconds)
                            if (_audioBuffer.Length > _waveIn.WaveFormat.AverageBytesPerSecond * 1.5)
                            {
                                // Stage 2: Confirmation with more sophisticated model
                                var audioData = _audioBuffer.ToArray();
                                if (await ConfirmWakeWordAsync(audioData))
                                {
                                    OnWakeWordDetected(audioData);
                                    _audioBuffer.SetLength(0); // Clear buffer after detection
                                }
                                else
                                {
                                    // Keep only the last second of audio for overlap detection
                                    var keepBytes = (int)(_waveIn.WaveFormat.AverageBytesPerSecond * 1.0);
                                    if (_audioBuffer.Length > keepBytes)
                                    {
                                        var keepBuffer = new byte[keepBytes];
                                        Array.Copy(
                                            _audioBuffer.GetBuffer(),
                                            _audioBuffer.Length - keepBytes,
                                            keepBuffer,
                                            0,
                                            keepBytes);
                                            
                                        _audioBuffer.SetLength(0);
                                        _audioBuffer.Write(keepBuffer, 0, keepBuffer.Length);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Not a potential wake word, keep a rolling buffer
                            _audioBuffer.Write(chunk, 0, chunk.Length);
                            
                            // Maintain buffer size (1 second rolling window)
                            if (_audioBuffer.Length > _waveIn.WaveFormat.AverageBytesPerSecond)
                            {
                                var keepBytes = (int)(_waveIn.WaveFormat.AverageBytesPerSecond * 1.0);
                                var keepBuffer = new byte[keepBytes];
                                Array.Copy(
                                    _audioBuffer.GetBuffer(),
                                    _audioBuffer.Length - keepBytes,
                                    keepBuffer,
                                    0,
                                    keepBytes);
                                    
                                _audioBuffer.SetLength(0);
                                _audioBuffer.Write(keepBuffer, 0, keepBuffer.Length);
                            }
                        }
                    }
                    finally
                    {
                        _processingLock.Release();
                    }
                }
                else
                {
                    // No audio to process, wait a bit
                    await Task.Delay(10, _processingCts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in wake word detection processing");
        }
    }
    
    private bool IsPotentialWakeWord(byte[] audioChunk)
    {
        // This is a simplified implementation
        // In a real implementation, this would use a lightweight energy detection
        // algorithm combined with simple pattern matching
        
        // 1. Check if audio energy is above threshold (indicating speech)
        if (!HasSufficientEnergy(audioChunk))
        {
            return false;
        }
        
        // 2. Apply very basic frequency pattern matching
        // This is just a placeholder for the actual algorithm
        return HasPotentialWakeWordPattern(audioChunk);
    }
    
    private bool HasSufficientEnergy(byte[] audioChunk)
    {
        // Convert bytes to samples
        var samples = new short[audioChunk.Length / 2];
        Buffer.BlockCopy(audioChunk, 0, samples, 0, audioChunk.Length);
        
        // Calculate RMS energy
        double sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        
        double rms = Math.Sqrt(sum / samples.Length);
        
        // Threshold can be adapted based on environment
        return rms > 500; // Arbitrary threshold, would be tuned in practice
    }
    
    private bool HasPotentialWakeWordPattern(byte[] audioChunk)
    {
        // In a real implementation, this would perform basic frequency analysis
        // to look for patterns matching the spectral characteristics of "Adept"
        // This is just a placeholder for the actual algorithm
        
        return true; // For demonstration purposes
    }
    
    private async Task<bool> ConfirmWakeWordAsync(byte[] audioData)
    {
        try
        {
            // Convert audio data to the format expected by the wake word model
            var processedAudio = PreprocessAudio(audioData);
            
            // Use the dedicated wake word model to confirm
            var confidence = await _wakeWordModel.DetectAsync(processedAudio);
            
            // Log the detection confidence for tuning
            _logger.LogDebug("Wake word detection confidence: {Confidence}", confidence);
            
            // Threshold can be configured
            return confidence > 0.85; // 85% confidence threshold
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in wake word confirmation");
            return false;
        }
    }
    
    private float[] PreprocessAudio(byte[] audioData)
    {
        // Convert bytes to PCM samples
        var samples = new short[audioData.Length / 2];
        Buffer.BlockCopy(audioData, 0, samples, 0, audioData.Length);
        
        // Convert to float format for processing
        var floatSamples = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            floatSamples[i] = samples[i] / 32768.0f; // Normalize to [-1, 1]
        }
        
        // In a real implementation, additional preprocessing steps would be applied:
        // - Pre-emphasis filter
        // - Framing
        // - Windowing
        // - Feature extraction (e.g., MFCC)
        
        return floatSamples;
    }
    
    private void OnWakeWordDetected(byte[] audioData)
    {
        _logger.LogInformation("Wake word 'Adept' detected");
        WakeWordDetected?.Invoke(this, new WakeWordDetectedEventArgs(audioData));
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _processingCts.Cancel();
            
            try
            {
                _processingTask?.Wait(1000);
            }
            catch (AggregateException)
            {
                // Ignore cancellation exceptions
            }
            
            _waveIn?.Dispose();
            _audioBuffer?.Dispose();
            _processingCts?.Dispose();
            _processingLock?.Dispose();
            
            _isDisposed = true;
        }
    }
}
```

### 9.3 Wake Word Model Interface

```csharp
public interface IWakeWordModel
{
    Task<float> DetectAsync(float[] audioData);
    Task InitializeAsync();
    bool IsInitialized { get; }
}
```

## 10. Voice Service Implementation

### 10.1 Voice Service Architecture

The Voice Service orchestrates the complete voice interaction pipeline:

1. Wake word detection
2. Audio capture for command
3. STT processing
4. Command routing to LLM
5. Response generation
6. TTS output

The implementation follows the state machine pattern to manage transitions between different states in the voice interaction lifecycle.

### 10.2 Voice Service Implementation

```csharp
public class VoiceService : IVoiceService, IDisposable
{
    private readonly IWakeWordDetector _wakeWordDetector;
    private readonly ISpeechToTextFactory _sttFactory;
    private readonly ITtsProvider _ttsProvider;
    private readonly ILlmService _llmService;
    private readonly ILlmContextManager _contextManager;
    private readonly ILogger<VoiceService> _logger;
    private readonly WaveInEvent _commandAudioCapture;
    private readonly MemoryStream _commandAudioBuffer;
    
    private VoiceServiceState _currentState = VoiceServiceState.Idle;
    private ISpeechToTextProvider _currentSttProvider;
    private CancellationTokenSource _commandCaptureCts;
    private bool _isDisposed;
    
    public event EventHandler<StateChangedEventArgs> StateChanged;
    public event EventHandler<CommandDetectedEventArgs> CommandDetected;
    public event EventHandler<ResponseGeneratedEventArgs> ResponseGenerated;
    
    public VoiceServiceState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnStateChanged(value);
            }
        }
    }
    
    public VoiceService(
        IWakeWordDetector wakeWordDetector,
        ISpeechToTextFactory sttFactory,
        ITtsProvider ttsProvider,
        ILlmService llmService,
        ILlmContextManager contextManager,
        ILogger<VoiceService> logger)
    {
        _wakeWordDetector = wakeWordDetector;
        _sttFactory = sttFactory;
        _ttsProvider = ttsProvider;
        _llmService = llmService;
        _contextManager = contextManager;
        _logger = logger;
        
        // Initialize audio capture for commands
        _commandAudioCapture = new WaveInEvent
        {
            DeviceNumber = -1, // Default device
            WaveFormat = new WaveFormat(16000, 1), // 16kHz mono
            BufferMilliseconds = 100
        };
        
        _commandAudioBuffer = new MemoryStream();
        _commandAudioCapture.DataAvailable += CommandAudioCapture_DataAvailable;
        _commandAudioCapture.RecordingStopped += CommandAudioCapture_RecordingStopped;
        
        // Subscribe to wake word detection
        _wakeWordDetector.WakeWordDetected += WakeWordDetector_WakeWordDetected;
    }
    
    public async Task StartAsync()
    {
        _logger.LogInformation("Starting voice service");
        
        // Initialize STT provider
        _currentSttProvider = await _sttFactory.CreateProviderAsync();
        
        // Start wake word detection
        _wakeWordDetector.Start();
        
        CurrentState = VoiceServiceState.Listening;
    }
    
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping voice service");
        
        // Stop wake word detection
        _wakeWordDetector.Stop();
        
        // Stop command capture if active
        await StopCommandCaptureAsync();
        
        CurrentState = VoiceServiceState.Idle;
    }
    
    private void WakeWordDetector_WakeWordDetected(object sender, WakeWordDetectedEventArgs e)
    {
        _logger.LogInformation("Wake word detected, starting command capture");
        
        // Transition to command capture state
        CurrentState = VoiceServiceState.CapturingCommand;
        
        // Start capturing the command
        StartCommandCapture();
    }
    
    private void StartCommandCapture()
    {
        // Clear previous audio
        _commandAudioBuffer.SetLength(0);
        
        // Initialize cancellation token for automatic timeout
        _commandCaptureCts = new CancellationTokenSource();
        
        // Start audio capture
        _commandAudioCapture.StartRecording();
        
        // Set a timeout to automatically stop recording after 10 seconds
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(10000, _commandCaptureCts.Token);
                
                // If we reach this point, timeout occurred
                if (CurrentState == VoiceServiceState.CapturingCommand)
                {
                    await StopCommandCaptureAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, ignore
            }
        });
        
        // Also detect silence to automatically stop recording
        Task.Run(async () =>
        {
            try
            {
                // Wait for initial speech (up to 3 seconds)
                await Task.Delay(3000, _commandCaptureCts.Token);
                
                // Now monitor for silence
                while (!_commandCaptureCts.Token.IsCancellationRequested)
                {
                    // Check last 500ms of audio for silence
                    if (IsAudioSilent() && CurrentState == VoiceServiceState.CapturingCommand)
                    {
                        await StopCommandCaptureAsync();
                        break;
                    }
                    
                    await Task.Delay(100, _commandCaptureCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, ignore
            }
        });
    }
    
    private async Task StopCommandCaptureAsync()
    {
        if (CurrentState == VoiceServiceState.CapturingCommand)
        {
            _logger.LogInformation("Stopping command capture");
            
            // Cancel the timeout task
            _commandCaptureCts?.Cancel();
            
            // Stop recording
            _commandAudioCapture.StopRecording();
            
            // Wait for the recording stopped event to complete processing
            CurrentState = VoiceServiceState.ProcessingCommand;
        }
    }
    
    private void CommandAudioCapture_DataAvailable(object sender, WaveInEventArgs e)
    {
        // Add audio data to buffer
        _commandAudioBuffer.Write(e.Buffer, 0, e.BytesRecorded);
    }
    
    private async void CommandAudioCapture_RecordingStopped(object sender, StoppedEventArgs e)
    {
        try
        {
            if (CurrentState != VoiceServiceState.ProcessingCommand)
            {
                return; // State was changed externally
            }
            
            _logger.LogInformation("Processing captured command audio");
            
            // Get audio data
            var audioData = _commandAudioBuffer.ToArray();
            
            // If too short, likely not a command
            if (audioData.Length < _commandAudioCapture.WaveFormat.AverageBytesPerSecond * 0.5)
            {
                _logger.LogInformation("Captured audio too short, returning to listening state");
                CurrentState = VoiceServiceState.Listening;
                return;
            }
            
            // Transcribe command
            string transcription = await _currentSttProvider.TranscribeAsync(audioData);
            
            if (string.IsNullOrWhiteSpace(transcription))
            {
                _logger.LogInformation("No transcription detected, returning to listening state");
                CurrentState = VoiceServiceState.Listening;
                return;
            }
            
            _logger.LogInformation("Command transcribed: {Transcription}", transcription);
            
            // Raise command detected event
            OnCommandDetected(transcription);
            
            // Process with LLM
            CurrentState = VoiceServiceState.GeneratingResponse;
            
            // Build context for the LLM
            var messages = await _contextManager.BuildContextAsync(transcription);
            
            // Get response from LLM
            string response = await _llmService.ProcessCommandAsync(messages);
            
            _logger.LogInformation("Response generated: {Response}", 
                response.Length > 100 ? response.Substring(0, 100) + "..." : response);
            
            // Raise response generated event
            OnResponseGenerated(response);
            
            // Convert to speech
            CurrentState = VoiceServiceState.Speaking;
            
            await _ttsProvider.SpeakAsync(response);
            
            // Return to listening state
            CurrentState = VoiceServiceState.Listening;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command");
            CurrentState = VoiceServiceState.Listening;
        }
    }
    
    private bool IsAudioSilent()
    {
        // This method analyzes the recent audio to detect silence
        // Implementation would check the energy level in the last portion of captured audio
        
        // For a real implementation, we would:
        // 1. Get the last 500ms of audio from _commandAudioBuffer
        // 2. Calculate the RMS energy
        // 3. Compare against a silence threshold
        // 4. Return true if below threshold for a certain duration
        
        return false; // Placeholder
    }
    
    private void OnStateChanged(VoiceServiceState newState)
    {
        _logger.LogDebug("Voice service state changed to: {State}", newState);
        StateChanged?.Invoke(this, new StateChangedEventArgs(newState));
    }
    
    private void OnCommandDetected(string command)
    {
        CommandDetected?.Invoke(this, new CommandDetectedEventArgs(command));
    }
    
    private void OnResponseGenerated(string response)
    {
        ResponseGenerated?.Invoke(this, new ResponseGeneratedEventArgs(response));
    }
    
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _commandAudioCapture?.Dispose();
            _commandAudioBuffer?.Dispose();
            _commandCaptureCts?.Dispose();
            
            if (_wakeWordDetector is IDisposable disposableDetector)
            {
                disposableDetector.Dispose();
            }
            
            _isDisposed = true;
        }
    }
}

public enum VoiceServiceState
{
    Idle,
    Listening,
    CapturingCommand,
    ProcessingCommand,
    GeneratingResponse,
    Speaking
}
```

## 11. Installation and Deployment

### 11.1 Installation Package

The application will be distributed as a Windows installer package created with WiX Toolset:

```xml
<!-- Adept.wxs -->
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Product Id="*" Name="Adept AI Teaching Assistant" Language="1033" 
             Version="1.0.0.0" Manufacturer="Your Organization" 
             UpgradeCode="12345678-1234-1234-1234-123456789012">
        
        <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
        
        <MajorUpgrade DowngradeErrorMessage="A newer version of Adept AI Teaching Assistant is already installed." />
        <MediaTemplate EmbedCab="yes" />
        
        <Feature Id="ProductFeature" Title="Adept AI Teaching Assistant" Level="1">
            <ComponentGroupRef Id="ProductComponents" />
            <ComponentGroupRef Id="McpServers" />
        </Feature>
        
        <UIRef Id="WixUI_InstallDir" />
        <Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER" />
        
        <WixVariable Id="WixUILicenseRtf" Value="License.rtf" />
        <WixVariable Id="WixUIBannerBmp" Value="banner.bmp" />
        <WixVariable Id="WixUIDialogBmp" Value="dialog.bmp" />
    </Product>
    
    <Fragment>
        <Directory Id="TARGETDIR" Name="SourceDir">
            <Directory Id="ProgramFilesFolder">
                <Directory Id="INSTALLFOLDER" Name="Adept AI Teaching Assistant">
                    <Directory Id="McpFolder" Name="mcp" />
                    <Directory Id="ConfigFolder" Name="config" />
                    <Directory Id="LogsFolder" Name="logs" />
                </Directory>
            </Directory>
            
            <Directory Id="ProgramMenuFolder">
                <Directory Id="ApplicationProgramsFolder" Name="Adept AI Teaching Assistant" />
            </Directory>
            
            <Directory Id="DesktopFolder" Name="Desktop" />
        </Directory>
    </Fragment>
    
    <Fragment>
        <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
            <Component Id="MainExecutable" Guid="*">
                <File Id="AdeptEXE" Source="$(var.Adept.TargetPath)" KeyPath="yes">
                    <Shortcut Id="ApplicationStartMenuShortcut" 
                              Directory="ApplicationProgramsFolder"
                              Name="Adept AI Teaching Assistant"
                              WorkingDirectory="INSTALLFOLDER"
                              Icon="AdeptIcon.ico"
                              Advertise="yes" />
                    <Shortcut Id="ApplicationDesktopShortcut"
                              Directory="DesktopFolder"
                              Name="Adept AI Teaching Assistant"
                              WorkingDirectory="INSTALLFOLDER"
                              Icon="AdeptIcon.ico"
                              Advertise="yes" />
                </File>
            </Component>
            
            <!-- Add components for all required assemblies -->
            <Component Id="Assemblies" Guid="*">
                <File Id="Assembly1" Source="$(var.Adept.TargetDir)Assembly1.dll" KeyPath="no" />
                <File Id="Assembly2" Source="$(var.Adept.TargetDir)Assembly2.dll" KeyPath="no" />
                <!-- Additional assemblies... -->
            </Component>
            
            <!-- Create default configuration folders -->
            <Component Id="CreateConfigFolder" Guid="*">
                <CreateFolder Directory="ConfigFolder" />
            </Component>
            
            <Component Id="CreateLogsFolder" Guid="*">
                <CreateFolder Directory="LogsFolder" />
            </Component>
            
            <!-- Add application icon -->
            <Component Id="ApplicationIcon" Guid="*">
                <File Id="AdeptIcon.ico" Source="$(var.Adept.ProjectDir)Resources\AdeptIcon.ico" KeyPath="yes" />
            </Component>
        </ComponentGroup>
        
        <ComponentGroup Id="McpServers" Directory="McpFolder">
            <!-- Add components for MCP servers -->
            <Component Id="FilesystemMcp" Guid="*">
                <File Id="FilesystemMcpEXE" Source="$(var.McpServersDir)filesystem\filesystem-mcp.exe" KeyPath="yes" />
                <!-- Additional files for this MCP server... -->
            </Component>
            
            <Component Id="BraveSearchMcp" Guid="*">
                <File Id="BraveSearchMcpEXE" Source="$(var.McpServersDir)brave-search\brave-search-mcp.exe" KeyPath="yes" />
                <!-- Additional files for this MCP server... -->
            </Component>
            
            <!-- Additional MCP server components... -->
        </ComponentGroup>
    </Fragment>
    
    <Fragment>
        <Icon Id="AdeptIcon.ico" SourceFile="$(var.Adept.ProjectDir)Resources\AdeptIcon.ico" />
    </Fragment>
</Wix>
```

### 11.2 First-Run Setup Wizard

The application will include a first-run setup wizard to guide users through initial configuration:

```csharp
public class SetupWizard : ISetupWizard
{
    private readonly IConfigurationService _configService;
    private readonly ISecureStorageService _secureStorage;
    private readonly IMcpServerManager _mcpManager;
    private readonly ILogger<SetupWizard> _logger;
    
    public SetupWizard(
        IConfigurationService configService,
        ISecureStorageService secureStorage,
        IMcpServerManager mcpManager,
        ILogger<SetupWizard> logger)
    {
        _configService = configService;
        _secureStorage = secureStorage;
        _mcpManager = mcpManager;
        _logger = logger;
    }
    
    public async Task<bool> RunSetupAsync()
    {
        try
        {
            // Check if first run
            var isFirstRun = await _configService.GetConfigurationValueAsync("IsFirstRun", "true");
            
            if (isFirstRun != "true")
            {
                return true; // Setup already completed
            }
            
            _logger.LogInformation("Running first-time setup wizard");
            
            // Show setup wizard UI
            var wizardViewModel = new SetupWizardViewModel(
                _configService,
                _secureStorage,
                _mcpManager);
                
            var wizardWindow = new SetupWizardWindow
            {
                DataContext = wizardViewModel
            };
            
            bool? result = wizardWindow.ShowDialog();
            
            if (result == true)
            {
                // Setup completed successfully
                await _configService.SetConfigurationValueAsync("IsFirstRun", "false");
                return true;
            }
            
            return false; // Setup was cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during setup wizard");
            return false;
        }
    }
}
```

### 11.3 Update Mechanism

```csharp
public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _configService;
    
    public UpdateService(
        ILogger<UpdateService> logger,
        HttpClient httpClient,
        IConfigurationService configService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configService = configService;
    }
    
    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            // Get current version
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            
            // Get update server URL
            var updateServerUrl = await _configService.GetConfigurationValueAsync(
                "UpdateServerUrl", 
                "https://example.com/updates/adept");
                
            // Check for updates
            var response = await _httpClient.GetAsync($"{updateServerUrl}/version.json");
            
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = false,
                    ErrorMessage = "Could not connect to update server"
                };
            }
            
            var versionInfo = await response.Content.ReadFromJsonAsync<VersionInfo>();
            
            if (versionInfo == null)
            {
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = false,
                    ErrorMessage = "Invalid version information"
                };
            }
            
            // Parse server version
            if (!Version.TryParse(versionInfo.Version, out var serverVersion))
            {
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = false,
                    ErrorMessage = "Invalid version format"
                };
            }
            
            // Compare versions
            bool isUpdateAvailable = serverVersion > currentVersion;
            
            return new UpdateCheckResult
            {
                IsUpdateAvailable = isUpdateAvailable,
                CurrentVersion = currentVersion.ToString(),
                AvailableVersion = serverVersion.ToString(),
                ReleaseNotes = versionInfo.ReleaseNotes,
                DownloadUrl = versionInfo.DownloadUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            
            return new UpdateCheckResult
            {
                IsUpdateAvailable = false,
                ErrorMessage = "Error checking for updates: " + ex.Message
            };
        }
    }
    
    public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl)
    {
        try
        {
            // Download update package
            var response = await _httpClient.GetAsync(downloadUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            
            // Determine installer path
            var tempPath = Path.GetTempPath();
            var installerPath = Path.Combine(tempPath, "AdeptUpdate.msi");
            
            // Save the installer
            using (var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write))
            {
                await response.Content.CopyToAsync(fileStream);
            }
            
            // Launch installer
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{installerPath}\" /passive",
                UseShellExecute = true
            };
            
            Process.Start(startInfo);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing update");
            return false;
        }
    }
}
```

## 12. Performance Optimization

### 12.1 Memory Management

The application will implement the following memory optimization strategies:

1. **Efficient Buffer Management**:
   - Reuse audio buffers where possible
   - Implement pooling for frequently used objects
   - Clear unused buffers promptly

2. **Resource Disposal**:
   - Implement proper IDisposable patterns
   - Use `using` statements for resource-bound operations
   - Explicitly dispose large objects

3. **Memory Monitoring**:
   - Track memory usage through performance counters
   - Implement soft limits with adaptive behavior
   - Log excessive memory usage

### 12.2 Threading and Asynchronous Operations

The application will use a structured approach to concurrency:

1. **UI Thread Management**:
   - Keep UI thread responsive
   - Use Progress<T> for reporting back to UI
   - Implement proper dispatching for cross-thread operations

2. **Background Operations**:
   - Use Task-based approach for I/O operations
   - Implement cancellation support for long-running tasks
   - Pool operations where appropriate

3. **Parallelism**:
   - Use parallel processing for compute-intensive operations
   - Limit degree of parallelism based on system capabilities
   - Monitor thread usage to prevent over-subscription

### 12.3 Startup Optimization

The application will implement efficient startup strategies:

1. **Lazy Initialization**:
   - Initialize components only when needed
   - Use deferred loading for non-critical features
   - Implement prioritized startup sequence

2. **Caching**:
   - Cache expensive resources
   - Implement warm start capabilities
   - Persist frequently used data between sessions

3. **Background Initialization**:
   - Start critical components first
   - Initialize non-essential services in background
   - Show functional UI before all systems are ready

## 13. Future Extension Points

### 13.1 LlamaParse Integration

The design includes extension points for future LlamaParse integration:

1. **Document Processing Service Interface**:
   ```csharp
   public interface IDocumentProcessingService
   {
       Task<ProcessedDocument> ProcessDocumentAsync(string filePath);
       Task<IEnumerable<string>> ExtractEntitiesAsync(ProcessedDocument document);
       Task<string> GenerateSummaryAsync(ProcessedDocument document);
   }
   ```

2. **Vector Database Integration Interface**:
   ```csharp
   public interface IVectorDatabaseService
   {
       Task<string> StoreEmbeddingAsync(string documentId, float[] embedding, Dictionary<string, object> metadata);
       Task<IEnumerable<SearchResult>> SearchAsync(float[] queryEmbedding, int limit = 5);
       Task DeleteDocumentAsync(string documentId);
   }
   ```

### 13.2 Student Assessment Analytics

The design includes extension points for future student assessment analytics:

1. **Assessment Data Import Interface**:
   ```csharp
   public interface IAssessmentDataImporter
   {
       Task<IEnumerable<AssessmentRecord>> ImportAssessmentDataAsync(string filePath);
       Task<IEnumerable<StudentAssessmentSummary>> GenerateStudentSummariesAsync(IEnumerable<AssessmentRecord> records);
       Task<ClassAssessmentSummary> GenerateClassSummaryAsync(IEnumerable<AssessmentRecord> records);
   }
   ```

2. **Reporting Service Interface**:
   ```csharp
   public interface IReportingService
   {
       Task<byte[]> GenerateStudentReportAsync(string studentId, ReportFormat format);
       Task<byte[]> GenerateClassReportAsync(string classId, ReportFormat format);
       Task<byte[]> GenerateProgressReportAsync(string studentId, string classId, DateRange dateRange, ReportFormat format);
   }
   ```

### 13.3 Extended Computer Control

The design includes extension points for future computer control capabilities:

1. **Application Control Interface**:
   ```csharp
   public interface IApplicationControlService
   {
       Task<bool> LaunchApplicationAsync(string applicationPath, string arguments = null);
       Task<bool> CloseApplicationAsync(string applicationName);
       Task<IEnumerable<RunningApplication>> GetRunningApplicationsAsync();
   }
   ```

2. **Presentation Control Interface**:
   ```csharp
   public interface IPresentationControlService
   {
       Task ConnectToPresentationAsync(string filePath);
       Task NextSlideAsync();
       Task PreviousSlideAsync();
       Task GoToSlideAsync(int slideNumber);
       Task<int> GetCurrentSlideNumberAsync();
       Task<int> GetTotalSlidesAsync();
   }
   ```

### 13.4 Web Application Migration

The design includes considerations for future web application migration:

1. **Service Layer Abstraction**:
   - All business logic is isolated in services
   - UI components are separated from core functionality
   - Database access is abstracted through repositories

2. **API-Ready Design**:
   - Service methods are async and return data, not UI components
   - Authentication/authorization is abstracted
   - Configuration is externalized

3. **Multi-tenancy Preparation**:
   - Data models include tenant identifiers
   - Services can be adapted for multi-user scenarios
   - Resource isolation is built into the design

## 14. Conclusion

This Design Document provides a comprehensive technical blueprint for implementing the Adept AI Teaching Assistant. It covers all aspects of the system from architecture to implementation details, providing clear guidance for development. The design follows best practices for software architecture, emphasizing modularity, extensibility, and maintainability.

The document addresses all requirements specified in the SRS, URS, and SysRS, translating them into concrete implementation plans. By following this design, developers can create a robust, feature-rich application that meets the needs of teachers for classroom assistance and lesson planning.

The design also incorporates forward-thinking considerations for future enhancements, ensuring that the application can evolve to accommodate new features and capabilities as requirements change over time.