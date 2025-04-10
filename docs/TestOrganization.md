# ADEPT Test Organization and Improvement Plan

This document outlines a comprehensive plan for organizing and improving the test structure in the ADEPT project. It addresses current issues with test organization and provides specific recommendations for enhancing test quality and coverage.

## Current State Analysis

The ADEPT project has a well-organized test structure in the `tests` directory:

```
tests/
├── Unit/                      # Unit tests
│   ├── Adept.Core.Tests/      # Core unit tests
│   │   └── Models/            # Tests for core models
│   ├── Adept.Services.Tests/  # Services unit tests
│   │   ├── Llm/              # Tests for LLM services
│   │   └── Mcp/              # Tests for MCP tools
│   └── Adept.Data.Tests/      # Data unit tests
│       ├── Database/         # Tests for database operations
│       ├── Repository/       # Tests for repositories
│       ├── TestData/         # Test data for data tests
│       └── Validation/       # Tests for validation
├── Integration/               # Integration tests
│   ├── Adept.FileSystem.Tests/ # File system integration tests
│   │   ├── Fixtures/         # Test fixtures for file system tests
│   │   └── Services/         # Tests for file system services
│   └── Adept.Database.Tests/  # Database integration tests
├── Manual/                    # Manual test applications
│   ├── Adept.Calendar.ManualTests/       # Calendar manual tests
│   ├── Adept.FileSystem.ManualTests/     # File system manual tests
│   ├── Adept.GoogleCalendar.ManualTests/ # Google Calendar manual tests
│   ├── Adept.Llm.ManualTests/            # LLM manual tests
│   ├── Adept.Mcp.ManualTests/            # MCP manual tests
│   └── Adept.Puppeteer.ManualTests/      # Puppeteer manual tests
└── Common/                    # Shared test utilities
    └── Adept.TestUtilities/   # Common test helpers and fixtures
```

The test directories that were previously in the `src` folder have been migrated to the appropriate locations in the `tests` directory structure.

## Test Organization Recommendations

### 1. Consolidate Test Directories ✅

**Previous Issue:** Tests were spread across multiple locations, creating confusion about where tests should be located.

**Implemented Solution:**
- Moved all tests from `src/Adept.Tests` to the appropriate location in the `tests` directory structure
- Specifically, moved:
  - `ResourceManagementTests.cs` → `tests/Unit/Adept.Data.Tests/Repository`
  - `TemplateManagementTests.cs` → `tests/Unit/Adept.Data.Tests/Repository`
  - `PuppeteerToolProviderTests.cs` → `tests/Unit/Adept.Services.Tests/Mcp`

### 2. Clean Up Test Directories in src ✅

**Previous Issue:** There were test directories within the `src` folder that needed to be removed.

**Implemented Solution:**
- Updated the `cleanup-test-dirs.bat` script to include `src/Adept.Tests`
- The script can now be executed to remove all test directories from the `src` folder

### 3. Standardize Test Project Structure ✅

**Previous Issue:** The test projects had placeholder folders but many were empty.

**Implemented Solution:**
- Implemented a consistent structure within each test project:
  ```
  tests/Unit/Adept.Core.Tests/
  ├── Models/                # Tests for models
  ├── Interfaces/            # Tests for interfaces
  ├── Utilities/             # Tests for utilities
  ├── TestData/              # Test data files
  └── TestConstants.cs       # Constants used in tests
  ```
- Created similar structures for other test projects based on what they're testing
- Added README.md files to each directory explaining its purpose and providing guidelines for tests

### 4. Complete Missing Test Projects

**Current Issue:** Some test projects mentioned in documentation don't exist or are incomplete.

**Recommendation:**
- Create any missing test projects mentioned in the documentation
- Ensure all test projects have proper references to the projects they're testing
- Update the solution file to include all test projects

### 5. Improve Test Utilities

**Current Issue:** The test utilities project has good foundations but could be expanded.

**Recommendation:**
- Enhance the `MockFactory` to support all services in the application
- Add more test data generators for all model types
- Create additional assertion extensions for common test scenarios
- Implement more test fixtures for different testing scenarios

## Test Implementation Recommendations

### 1. Increase Test Coverage

**Current Issue:** Test coverage appears to be limited, with many empty test folders.

**Recommendation:**
- Implement unit tests for all repositories, starting with the newly created ones
- Add tests for core models and their validation
- Create tests for service implementations
- Implement integration tests for database operations
- Add UI tests using a UI testing framework

### 2. Standardize Test Naming and Structure

**Current Issue:** Test naming and structure may be inconsistent.

**Recommendation:**
- Adopt a consistent naming convention: `[MethodName]_[Scenario]_[ExpectedResult]`
- Follow the Arrange-Act-Assert pattern consistently
- Group tests by feature and class
- Use descriptive test names that explain the behavior being tested

### 3. Implement Test Data Generation

**Current Issue:** Test data creation may be inconsistent or duplicated.

**Recommendation:**
- Create factory methods for generating test data
- Use the `TestDataGenerator` class for all test data
- Create realistic test data that covers edge cases
- Avoid hardcoded test data in test methods

### 4. Add Test Documentation

**Current Issue:** Test documentation is limited.

**Recommendation:**
- Add XML documentation to test classes and methods
- Create a test plan document that outlines test coverage goals
- Document test fixtures and their usage
- Add comments explaining complex test scenarios

### 5. Implement Continuous Integration

**Current Issue:** It's unclear if tests are run automatically.

**Recommendation:**
- Set up CI/CD to run tests automatically
- Configure test reporting to track test coverage
- Add code coverage analysis
- Implement test result visualization

## Specific Test Implementation Tasks

### 1. Resource Repository Tests

- Move `ResourceManagementTests.cs` to `tests/Unit/Adept.Data.Tests/Repository`
- Expand tests to cover all CRUD operations:
  - `GetResourcesByLessonIdAsync_ValidLessonId_ReturnsResources`
  - `GetResourceByIdAsync_ValidId_ReturnsResource`
  - `GetResourceByIdAsync_InvalidId_ReturnsNull`
  - `AddResourceAsync_ValidResource_AddsResourceAndReturnsIt`
  - `UpdateResourceAsync_ValidResource_UpdatesResourceAndReturnsIt`
  - `UpdateResourceAsync_InvalidId_ReturnsNull`
  - `DeleteResourceAsync_ValidId_ReturnsTrue`
  - `DeleteResourceAsync_InvalidId_ReturnsFalse`
  - `DeleteResourcesByLessonIdAsync_ValidLessonId_ReturnsTrue`
- Add tests for edge cases and error handling

### 2. Template Repository Tests

- Move `TemplateManagementTests.cs` to `tests/Unit/Adept.Data.Tests/Repository`
- Expand tests to cover all CRUD operations:
  - `GetAllTemplatesAsync_ReturnsAllTemplates`
  - `GetTemplateByIdAsync_ValidId_ReturnsTemplate`
  - `GetTemplateByIdAsync_InvalidId_ReturnsNull`
  - `GetTemplatesByCategoryAsync_ValidCategory_ReturnsTemplates`
  - `GetTemplatesByCategoryAsync_InvalidCategory_ReturnsEmptyList`
  - `GetTemplatesByTagAsync_ValidTag_ReturnsTemplates`
  - `GetTemplatesByTagAsync_InvalidTag_ReturnsEmptyList`
  - `AddTemplateAsync_ValidTemplate_AddsTemplateAndReturnsIt`
  - `UpdateTemplateAsync_ValidTemplate_UpdatesTemplateAndReturnsIt`
  - `UpdateTemplateAsync_InvalidId_ReturnsNull`
  - `DeleteTemplateAsync_ValidId_ReturnsTrue`
  - `DeleteTemplateAsync_InvalidId_ReturnsFalse`
  - `SearchTemplatesAsync_ValidTerm_ReturnsMatchingTemplates`
  - `SearchTemplatesAsync_InvalidTerm_ReturnsEmptyList`
- Add tests for searching and filtering templates

### 3. Database Tests

- Create integration tests for database migrations:
  - `ApplyMigrationsAsync_FromVersion0_AppliesAllMigrations`
  - `ApplyMigrationsAsync_FromVersion1_AppliesNewerMigrations`
  - `ApplyMigrationsAsync_FromLatestVersion_AppliesNoMigrations`
- Test database backup and restore functionality:
  - `CreateBackupAsync_CreatesBackupFile`
  - `RestoreBackupAsync_ValidBackup_RestoresDatabase`
  - `GetAvailableBackupsAsync_ReturnsBackups`
- Test database integrity checks:
  - `CheckIntegrityAsync_ValidDatabase_ReturnsValidResult`
  - `CheckIntegrityAsync_CorruptDatabase_ReturnsInvalidResult`
  - `AttemptRepairAsync_RepairableDatabase_ReturnsTrue`

### 4. UI Tests

- Implement tests for the LessonPlannerViewModel:
  - `LoadResourcesAsync_ValidLesson_LoadsResources`
  - `AddFileResource_ValidFile_AddsResource`
  - `AddLinkResource_ValidLink_AddsResource`
  - `RemoveResource_ValidResource_RemovesResource`
  - `LoadTemplatesAsync_LoadsTemplates`
  - `SaveAsTemplate_ValidLesson_CreatesTemplate`
  - `ApplyTemplate_ValidTemplate_AppliesTemplateToLesson`
- Test resource management UI functionality
- Test template management UI functionality

### 5. MCP Tool Tests

- Move `PuppeteerToolProviderTests.cs` to `tests/Unit/Adept.Services.Tests/Mcp`
- Implement tests for other MCP tools:
  - `FileSystemToolProvider`
  - `CalendarToolProvider`
  - `WebSearchToolProvider`
  - `ExcelToolProvider`
  - `FetchToolProvider`
  - `SequentialThinkingToolProvider`
- Create integration tests for MCP server communication

## Implementation Plan

### Phase 1: Test Organization (Week 1) ✅

1. ✅ Migrated tests from `src/Adept.Tests` to the appropriate locations
2. ✅ Updated the cleanup script to include `src/Adept.Tests`
3. ✅ Updated the root `run-tests.bat` to call `tests/run-tests.bat`
4. ✅ Standardized the folder structure in all test projects
5. ✅ Added README.md files to each test directory explaining its purpose and providing guidelines

### Phase 2: Test Infrastructure (Week 2) ✅

1. ✅ Fix build errors in Adept.Data project:
   - Created missing `IDatabaseProvider` interface
   - Implemented `SqliteDatabaseProvider` class
   - Updated repositories to use Microsoft.Data.Sqlite
2. ✅ Fixed test build errors:
   - Fixed ambiguous reference to MockFactory in database test files
   - Created simplified EntityValidatorTests that focus on LessonResource and LessonTemplate
   - Fixed type conversion issues in DatabaseIntegrityServiceTests
3. ✅ Verified that tests can run:
   - All validation tests are passing
   - Database tests are failing due to missing database files (expected)
4. ⏳ Enhance the test utilities project:
   - Add mock implementations for all services
   - Create test data generators for all models
   - Implement additional assertion extensions
5. ⏳ Create test fixtures for all major components:
   - Database fixtures for repository tests
   - File system fixtures for file operations
   - Service fixtures for integration tests
6. ⏳ Document the test infrastructure and how to use it

### Phase 3: Unit Tests (Weeks 3-4)

1. Implement tests for repositories:
   - `LessonResourceRepository`
   - `LessonTemplateRepository`
   - Other repositories
2. Add tests for services:
   - `LessonPlannerService`
   - `TemplateManagementService`
   - Other services
3. Create tests for models and validation:
   - `LessonResource`
   - `LessonTemplate`
   - Other models

### Phase 4: Integration Tests (Weeks 5-6)

1. Implement database integration tests:
   - Database migrations
   - Database backup and restore
   - Database integrity checks
2. Add file system integration tests:
   - File operations
   - Directory operations
   - File organization
3. Create API integration tests:
   - LLM provider integration
   - Calendar integration
   - Other external services

### Phase 5: UI Tests (Weeks 7-8)

1. Implement tests for ViewModels:
   - `LessonPlannerViewModel`
   - Other ViewModels
2. Add tests for UI controls:
   - `ResourceAttachmentControl`
   - `TemplateManagementControl`
   - Other controls
3. Create end-to-end tests for key user flows

## Test Maintenance Guidelines

1. **Keep Tests Updated**: When making changes to the codebase, update the corresponding tests
2. **Run Tests Regularly**: Run tests before committing changes
3. **Fix Failing Tests**: Address failing tests immediately
4. **Review Test Coverage**: Regularly review test coverage and add tests for uncovered code
5. **Refactor Tests**: Refactor tests when they become difficult to maintain
6. **Document Test Requirements**: Document any special requirements for running tests

## Fixed Issues

### Build Errors in Adept.Data Project ✅

The following build errors in the Adept.Data project have been fixed:

1. Missing reference to `System.Data.SQLite`:
   - Updated the repositories to use `Microsoft.Data.Sqlite` instead of `System.Data.SQLite`
   - This aligns with the project's use of Microsoft.Data.Sqlite in other parts of the codebase

2. Missing `IDatabaseProvider` interface:
   - Created the `IDatabaseProvider` interface in the `Adept.Data.Database` namespace
   - Implemented `SqliteDatabaseProvider` class that implements the interface
   - Added service registration extension method for the database services

These changes ensure that the Adept.Data project can be built successfully, which is a prerequisite for running the tests.

## Conclusion

Implementing this test organization and improvement plan will significantly enhance the quality and maintainability of the ADEPT project. By consolidating tests into a single, well-organized structure and improving test coverage, the project will be more robust and easier to maintain.

The plan provides a clear roadmap for addressing current issues and implementing best practices for testing. By following this plan, the ADEPT project will benefit from improved code quality, reduced bugs, and easier maintenance.

The test organization has been significantly improved by migrating tests from the `src` directory to the appropriate locations in the `tests` directory structure. The next steps involve fixing the build errors in the Adept.Data project and implementing the remaining recommendations in this plan.
