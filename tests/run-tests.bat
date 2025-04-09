@echo off
echo Running ADEPT Tests
echo =================

echo.
echo Running Unit Tests
echo -----------------
dotnet test tests\Unit\Adept.Core.Tests
dotnet test tests\Unit\Adept.Services.Tests
dotnet test tests\Unit\Adept.Data.Tests

echo.
echo Running Integration Tests
echo -----------------------
dotnet test tests\Integration\Adept.FileSystem.Tests
dotnet test tests\Integration\Adept.Database.Tests

echo.
echo Tests completed.
echo.
echo To run manual tests, use the following commands:
echo dotnet run --project tests\Manual\Adept.FileSystem.ManualTests
echo dotnet run --project tests\Manual\Adept.Llm.ManualTests
echo dotnet run --project tests\Manual\Adept.Calendar.ManualTests
echo dotnet run --project tests\Manual\Adept.GoogleCalendar.ManualTests
echo dotnet run --project tests\Manual\Adept.Mcp.ManualTests
echo dotnet run --project tests\Manual\Adept.Puppeteer.ManualTests
echo.

pause
