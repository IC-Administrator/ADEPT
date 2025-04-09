using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Adept.Mcp.ManualTests
{
    /// <summary>
    /// Manual tests for MCP tools
    /// </summary>
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static ILogger<Program> _logger = null!;
        private static HttpClient _httpClient = null!;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Adept MCP Tools Manual Tests");
            Console.WriteLine("===========================");

            // Initialize services
            InitializeServices();

            // Get services
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            _httpClient = _serviceProvider.GetRequiredService<HttpClient>();

            // Set up EPPlus license
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Display menu
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nMCP Tools Test Menu:");
                Console.WriteLine("1. Test Web Search");
                Console.WriteLine("2. Test Excel Operations");
                Console.WriteLine("3. Test Sequential Thinking");
                Console.WriteLine("4. Test Puppeteer");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await TestWebSearchAsync();
                            break;
                        case "2":
                            await TestExcelOperationsAsync();
                            break;
                        case "3":
                            await TestSequentialThinkingAsync();
                            break;
                        case "4":
                            await TestPuppeteerAsync();
                            break;
                        case "0":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during test execution");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            Console.WriteLine("\nTests completed. Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Initialize the services
        /// </summary>
        private static void InitializeServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add HTTP client
            services.AddHttpClient();

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Test web search functionality
        /// </summary>
        private static async Task TestWebSearchAsync()
        {
            Console.WriteLine("Testing Web Search");
            Console.WriteLine("=================\n");

            // Ask for Brave API key
            Console.Write("Enter your Brave API key (or press Enter to use a sample response): ");
            string apiKey = Console.ReadLine() ?? "";

            // Ask for search query
            Console.Write("Enter a search query (default: 'Model Context Protocol'): ");
            string query = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                query = "Model Context Protocol";
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // Use sample response
                Console.WriteLine("\nUsing sample response...");
                string sampleResponsePath = Path.Combine("TestFiles", "brave_search_response.json");
                if (File.Exists(sampleResponsePath))
                {
                    string jsonResponse = await File.ReadAllTextAsync(sampleResponsePath);
                    DisplaySearchResults(jsonResponse);
                }
                else
                {
                    Console.WriteLine($"Sample response file not found: {sampleResponsePath}");
                }
            }
            else
            {
                // Make a real API call
                Console.WriteLine($"\nSearching for: {query}");
                string jsonResponse = await SearchBraveAsync(query, apiKey);
                DisplaySearchResults(jsonResponse);
            }
        }

        /// <summary>
        /// Search using the Brave API
        /// </summary>
        private static async Task<string> SearchBraveAsync(string query, string apiKey)
        {
            string url = $"https://api.search.brave.com/res/v1/web/search?q={Uri.EscapeDataString(query)}&count=5";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Subscription-Token", apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Display search results
        /// </summary>
        private static void DisplaySearchResults(string jsonResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                // Display web results
                if (root.TryGetProperty("web", out var webElement) && 
                    webElement.TryGetProperty("results", out var resultsElement))
                {
                    Console.WriteLine("\nWeb Results:");
                    foreach (var result in resultsElement.EnumerateArray())
                    {
                        if (result.TryGetProperty("title", out var titleElement) &&
                            result.TryGetProperty("url", out var urlElement) &&
                            result.TryGetProperty("description", out var descriptionElement))
                        {
                            Console.WriteLine($"- {titleElement.GetString()}");
                            Console.WriteLine($"  URL: {urlElement.GetString()}");
                            Console.WriteLine($"  Description: {descriptionElement.GetString()}");
                            Console.WriteLine();
                        }
                    }
                }

                // Display video results
                if (root.TryGetProperty("videos", out var videosElement) && 
                    videosElement.TryGetProperty("results", out var videoResultsElement))
                {
                    Console.WriteLine("\nVideo Results:");
                    foreach (var result in videoResultsElement.EnumerateArray())
                    {
                        if (result.TryGetProperty("title", out var titleElement) &&
                            result.TryGetProperty("url", out var urlElement) &&
                            result.TryGetProperty("description", out var descriptionElement))
                        {
                            Console.WriteLine($"- {titleElement.GetString()}");
                            Console.WriteLine($"  URL: {urlElement.GetString()}");
                            Console.WriteLine($"  Description: {descriptionElement.GetString()}");
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Test Excel operations
        /// </summary>
        private static async Task TestExcelOperationsAsync()
        {
            Console.WriteLine("Testing Excel Operations");
            Console.WriteLine("=======================\n");

            // Create a temporary file path
            string filePath = Path.Combine(Path.GetTempPath(), $"ExcelTest_{Guid.NewGuid():N}.xlsx");

            try
            {
                // Create a new Excel file
                Console.WriteLine($"Creating Excel file: {filePath}");
                await CreateExcelFileAsync(filePath);

                // Read the Excel file
                Console.WriteLine("\nReading Excel file:");
                await ReadExcelFileAsync(filePath);

                // Update the Excel file
                Console.WriteLine("\nUpdating Excel file:");
                await UpdateExcelFileAsync(filePath);

                // Read the updated Excel file
                Console.WriteLine("\nReading updated Excel file:");
                await ReadExcelFileAsync(filePath);
            }
            finally
            {
                // Clean up
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"\nDeleted temporary file: {filePath}");
                }
            }
        }

        /// <summary>
        /// Create an Excel file
        /// </summary>
        private static async Task CreateExcelFileAsync(string filePath)
        {
            using var package = new ExcelPackage();
            
            // Add a worksheet
            var worksheet = package.Workbook.Worksheets.Add("Sample Data");
            
            // Add headers
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Age";
            worksheet.Cells[1, 4].Value = "Email";
            
            // Add data
            for (int i = 0; i < 10; i++)
            {
                worksheet.Cells[i + 2, 1].Value = i + 1;
                worksheet.Cells[i + 2, 2].Value = $"Person {i + 1}";
                worksheet.Cells[i + 2, 3].Value = 20 + i;
                worksheet.Cells[i + 2, 4].Value = $"person{i + 1}@example.com";
            }
            
            // Add a formula
            worksheet.Cells[12, 3].Formula = "AVERAGE(C2:C11)";
            
            // Save the file
            await package.SaveAsAsync(new FileInfo(filePath));
        }

        /// <summary>
        /// Read an Excel file
        /// </summary>
        private static async Task ReadExcelFileAsync(string filePath)
        {
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            
            // Get dimensions
            var dimension = worksheet.Dimension;
            int rows = dimension.Rows;
            int columns = dimension.Columns;
            
            Console.WriteLine($"Worksheet: {worksheet.Name}");
            Console.WriteLine($"Dimensions: {rows} rows x {columns} columns");
            
            // Read headers
            Console.WriteLine("\nHeaders:");
            for (int col = 1; col <= columns; col++)
            {
                Console.Write($"{worksheet.Cells[1, col].Value,-15}");
            }
            Console.WriteLine();
            
            // Read data (first 5 rows)
            Console.WriteLine("\nData (first 5 rows):");
            for (int row = 2; row <= Math.Min(rows, 6); row++)
            {
                for (int col = 1; col <= columns; col++)
                {
                    Console.Write($"{worksheet.Cells[row, col].Value,-15}");
                }
                Console.WriteLine();
            }
            
            // Read formula
            if (rows >= 12 && columns >= 3)
            {
                Console.WriteLine($"\nFormula at C12: {worksheet.Cells[12, 3].Formula}");
                Console.WriteLine($"Formula result: {worksheet.Cells[12, 3].Value}");
            }
        }

        /// <summary>
        /// Update an Excel file
        /// </summary>
        private static async Task UpdateExcelFileAsync(string filePath)
        {
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            
            // Update some data
            worksheet.Cells[2, 2].Value = "Updated Person 1";
            worksheet.Cells[2, 3].Value = 99;
            
            // Add a new row
            int lastRow = worksheet.Dimension.Rows;
            worksheet.Cells[lastRow + 1, 1].Value = lastRow;
            worksheet.Cells[lastRow + 1, 2].Value = $"New Person {lastRow}";
            worksheet.Cells[lastRow + 1, 3].Value = 30;
            worksheet.Cells[lastRow + 1, 4].Value = $"newperson{lastRow}@example.com";
            
            // Add a new column
            worksheet.Cells[1, 5].Value = "Status";
            for (int row = 2; row <= lastRow + 1; row++)
            {
                worksheet.Cells[row, 5].Value = row % 2 == 0 ? "Active" : "Inactive";
            }
            
            // Update the formula to include the new row
            worksheet.Cells[12, 3].Formula = $"AVERAGE(C2:C{lastRow + 1})";
            
            // Save the file
            await package.SaveAsAsync(new FileInfo(filePath));
        }

        /// <summary>
        /// Test sequential thinking
        /// </summary>
        private static async Task TestSequentialThinkingAsync()
        {
            Console.WriteLine("Testing Sequential Thinking");
            Console.WriteLine("==========================\n");

            // Define a complex problem
            string problem = "Calculate the sum of all even numbers between 1 and 100.";
            Console.WriteLine($"Problem: {problem}");

            // Solve the problem step by step
            Console.WriteLine("\nSolving step by step:");
            
            // Step 1: Identify the even numbers between 1 and 100
            Console.WriteLine("\nStep 1: Identify the even numbers between 1 and 100");
            Console.WriteLine("Even numbers are: 2, 4, 6, 8, ..., 98, 100");
            
            // Step 2: Express the sum as a formula
            Console.WriteLine("\nStep 2: Express the sum as a formula");
            Console.WriteLine("Sum = 2 + 4 + 6 + 8 + ... + 98 + 100");
            Console.WriteLine("This is an arithmetic sequence with:");
            Console.WriteLine("- First term (a) = 2");
            Console.WriteLine("- Last term (l) = 100");
            Console.WriteLine("- Number of terms (n) = 50 (because there are 50 even numbers between 1 and 100)");
            Console.WriteLine("- Common difference (d) = 2");
            
            // Step 3: Apply the formula for the sum of an arithmetic sequence
            Console.WriteLine("\nStep 3: Apply the formula for the sum of an arithmetic sequence");
            Console.WriteLine("Sum = n/2 * (a + l)");
            Console.WriteLine("Sum = 50/2 * (2 + 100)");
            Console.WriteLine("Sum = 25 * 102");
            Console.WriteLine("Sum = 2550");
            
            // Step 4: Verify the result
            Console.WriteLine("\nStep 4: Verify the result using a different approach");
            Console.WriteLine("Sum of numbers from 1 to 100 = 100 * 101 / 2 = 5050");
            Console.WriteLine("Sum of odd numbers from 1 to 99 = 50 * 100 / 2 = 2500");
            Console.WriteLine("Sum of even numbers = 5050 - 2500 = 2550");
            
            // Final answer
            Console.WriteLine("\nFinal answer: 2550");
            
            // Demonstrate another example
            Console.WriteLine("\n\nAnother example:");
            problem = "Find the derivative of f(x) = x³ - 4x² + 5x - 7";
            Console.WriteLine($"Problem: {problem}");
            
            // Step 1: Identify the terms and their derivatives
            Console.WriteLine("\nStep 1: Identify the terms and their derivatives");
            Console.WriteLine("f(x) = x³ - 4x² + 5x - 7");
            Console.WriteLine("Term 1: x³ → derivative: 3x²");
            Console.WriteLine("Term 2: -4x² → derivative: -8x");
            Console.WriteLine("Term 3: 5x → derivative: 5");
            Console.WriteLine("Term 4: -7 → derivative: 0");
            
            // Step 2: Combine the derivatives
            Console.WriteLine("\nStep 2: Combine the derivatives");
            Console.WriteLine("f'(x) = 3x² - 8x + 5");
            
            // Final answer
            Console.WriteLine("\nFinal answer: f'(x) = 3x² - 8x + 5");
        }

        /// <summary>
        /// Test Puppeteer
        /// </summary>
        private static async Task TestPuppeteerAsync()
        {
            Console.WriteLine("Testing Puppeteer");
            Console.WriteLine("================\n");

            // Download the browser if needed
            Console.WriteLine("Downloading browser if needed...");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Launch the browser
            Console.WriteLine("Launching browser...");
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                DefaultViewport = new ViewPortOptions
                {
                    Width = 1280,
                    Height = 800
                }
            });

            try
            {
                // Create a new page
                Console.WriteLine("Creating new page...");
                var page = await browser.NewPageAsync();

                // Navigate to a website
                string url = "https://www.example.com";
                Console.WriteLine($"Navigating to {url}...");
                await page.GoToAsync(url);

                // Get the page title
                string title = await page.GetTitleAsync();
                Console.WriteLine($"Page title: {title}");

                // Take a screenshot
                string screenshotPath = Path.Combine(Path.GetTempPath(), "puppeteer_screenshot.png");
                Console.WriteLine($"Taking screenshot: {screenshotPath}");
                await page.ScreenshotAsync(new ScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });

                // Get page content
                Console.WriteLine("\nGetting page content...");
                string content = await page.GetContentAsync();
                Console.WriteLine($"Page content (first 200 chars): {content.Substring(0, Math.Min(200, content.Length))}...");

                // Extract information
                Console.WriteLine("\nExtracting information...");
                var h1Text = await page.EvaluateExpressionAsync<string>("document.querySelector('h1').innerText");
                Console.WriteLine($"H1 text: {h1Text}");

                var paragraphText = await page.EvaluateExpressionAsync<string>("document.querySelector('p').innerText");
                Console.WriteLine($"First paragraph text: {paragraphText}");

                // Fill a form (if available)
                Console.WriteLine("\nChecking for forms...");
                var hasForm = await page.EvaluateExpressionAsync<bool>("document.forms.length > 0");
                if (hasForm)
                {
                    Console.WriteLine("Form found. Filling form...");
                    await page.TypeAsync("input[type=text]", "Test input");
                    await page.ClickAsync("input[type=submit]");
                    await page.WaitForNavigationAsync();
                    Console.WriteLine("Form submitted.");
                }
                else
                {
                    Console.WriteLine("No forms found on this page.");
                }

                // Navigate to another page
                url = "https://www.google.com";
                Console.WriteLine($"\nNavigating to {url}...");
                await page.GoToAsync(url);

                // Get the page title
                title = await page.GetTitleAsync();
                Console.WriteLine($"Page title: {title}");

                // Search for something
                Console.WriteLine("\nSearching for 'Puppeteer C#'...");
                await page.TypeAsync("input[name=q]", "Puppeteer C#");
                await page.Keyboard.PressAsync("Enter");
                await page.WaitForNavigationAsync();

                // Get search results
                Console.WriteLine("\nGetting search results...");
                var searchResults = await page.EvaluateExpressionAsync<string[]>(
                    "Array.from(document.querySelectorAll('h3')).map(h => h.innerText)");
                
                Console.WriteLine($"Found {searchResults.Length} search results:");
                for (int i = 0; i < Math.Min(5, searchResults.Length); i++)
                {
                    Console.WriteLine($"- {searchResults[i]}");
                }
            }
            finally
            {
                // Close the browser
                Console.WriteLine("\nClosing browser...");
                await browser.CloseAsync();
            }
        }
    }
}
