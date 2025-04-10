@echo off
echo Cleaning up legacy test directories
echo ================================
echo This script removes old test directories that have been migrated to the new test structure.

echo.
echo Removing src\CalendarApiTest...
if exist src\CalendarApiTest (
    rd /s /q src\CalendarApiTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing src\CalendarSyncTest...
if exist src\CalendarSyncTest (
    rd /s /q src\CalendarSyncTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing src\FileSystemTest...
if exist src\FileSystemTest (
    rd /s /q src\FileSystemTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing src\McpToolsTest...
if exist src\McpToolsTest (
    rd /s /q src\McpToolsTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing src\GoogleCalendarTest...
if exist src\GoogleCalendarTest (
    rd /s /q src\GoogleCalendarTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing src\PuppeteerTest...
if exist src\PuppeteerTest (
    rd /s /q src\PuppeteerTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing src\Adept.Tests...
if exist src\Adept.Tests (
    rd /s /q src\Adept.Tests
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo.
echo Removing CalendarIntegrationTest...
if exist CalendarIntegrationTest (
    rd /s /q CalendarIntegrationTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Removing RecurringEventTest...
if exist RecurringEventTest (
    rd /s /q RecurringEventTest
    echo - Removed successfully.
) else (
    echo - Directory not found.
)

echo.
echo Cleanup completed.
echo.

pause
