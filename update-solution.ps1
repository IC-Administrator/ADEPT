# Update the solution file to include all test projects

# Source projects
$sourceProjects = @(
    "src\Adept.Common\Adept.Common.csproj",
    "src\Adept.Core\Adept.Core.csproj",
    "src\Adept.Data\Adept.Data.csproj",
    "src\Adept.Services\Adept.Services.csproj",
    "src\Adept.UI\Adept.UI.csproj"
)

# Test utility projects
$testUtilityProjects = @(
    "tests\Common\Adept.TestUtilities\Adept.TestUtilities.csproj"
)

# Unit test projects
$unitTestProjects = @(
    "tests\Unit\Adept.Core.Tests\Adept.Core.Tests.csproj",
    "tests\Unit\Adept.Services.Tests\Adept.Services.Tests.csproj",
    "tests\Unit\Adept.Data.Tests\Adept.Data.Tests.csproj"
)

# Integration test projects
$integrationTestProjects = @(
    "tests\Integration\Adept.FileSystem.Tests\Adept.FileSystem.Tests.csproj",
    "tests\Integration\Adept.Database.Tests\Adept.Database.Tests.csproj"
)

# Manual test projects
$manualTestProjects = @(
    "tests\Manual\Adept.FileSystem.ManualTests\Adept.FileSystem.ManualTests.csproj",
    "tests\Manual\Adept.Llm.ManualTests\Adept.Llm.ManualTests.csproj",
    "tests\Manual\Adept.Calendar.ManualTests\Adept.Calendar.ManualTests.csproj",
    "tests\Manual\Adept.GoogleCalendar.ManualTests\Adept.GoogleCalendar.ManualTests.csproj",
    "tests\Manual\Adept.Mcp.ManualTests\Adept.Mcp.ManualTests.csproj",
    "tests\Manual\Adept.Puppeteer.ManualTests\Adept.Puppeteer.ManualTests.csproj"
)

# Create solution folders
Write-Host "Creating solution folders..."
dotnet sln Adept.Solution.sln add-folder src
dotnet sln Adept.Solution.sln add-folder tests
dotnet sln Adept.Solution.sln add-folder tests/Common
dotnet sln Adept.Solution.sln add-folder tests/Unit
dotnet sln Adept.Solution.sln add-folder tests/Integration
dotnet sln Adept.Solution.sln add-folder tests/Manual

# Add source projects
Write-Host "Adding source projects..."
foreach ($project in $sourceProjects) {
    if (Test-Path $project) {
        Write-Host "  Adding $project"
        dotnet sln Adept.Solution.sln add $project --solution-folder src
    } else {
        Write-Host "  Project not found: $project" -ForegroundColor Yellow
    }
}

# Add test utility projects
Write-Host "Adding test utility projects..."
foreach ($project in $testUtilityProjects) {
    if (Test-Path $project) {
        Write-Host "  Adding $project"
        dotnet sln Adept.Solution.sln add $project --solution-folder tests/Common
    } else {
        Write-Host "  Project not found: $project" -ForegroundColor Yellow
    }
}

# Add unit test projects
Write-Host "Adding unit test projects..."
foreach ($project in $unitTestProjects) {
    if (Test-Path $project) {
        Write-Host "  Adding $project"
        dotnet sln Adept.Solution.sln add $project --solution-folder tests/Unit
    } else {
        Write-Host "  Project not found: $project" -ForegroundColor Yellow
    }
}

# Add integration test projects
Write-Host "Adding integration test projects..."
foreach ($project in $integrationTestProjects) {
    if (Test-Path $project) {
        Write-Host "  Adding $project"
        dotnet sln Adept.Solution.sln add $project --solution-folder tests/Integration
    } else {
        Write-Host "  Project not found: $project" -ForegroundColor Yellow
    }
}

# Add manual test projects
Write-Host "Adding manual test projects..."
foreach ($project in $manualTestProjects) {
    if (Test-Path $project) {
        Write-Host "  Adding $project"
        dotnet sln Adept.Solution.sln add $project --solution-folder tests/Manual
    } else {
        Write-Host "  Project not found: $project" -ForegroundColor Yellow
    }
}

Write-Host "Solution update completed."
