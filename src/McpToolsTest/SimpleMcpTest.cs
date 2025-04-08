using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpToolsTest
{
    class SimpleMcpTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Simple MCP Tools Test");
            Console.WriteLine("====================");

            // Create test directory
            var testDir = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles");
            Directory.CreateDirectory(testDir);

            // Test FileSystem operations
            await TestFileSystemOperationsAsync(testDir);

            // Test Fetch operations
            await TestFetchOperationsAsync();

            // Test Puppeteer operations
            await TestPuppeteerOperationsAsync(testDir);

            // Test Brave Search operations
            await TestBraveSearchOperationsAsync();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestFileSystemOperationsAsync(string testDir)
        {
            Console.WriteLine("\nTesting FileSystem Operations:");
            Console.WriteLine("-----------------------------");

            try
            {
                // Write a file
                Console.WriteLine("Writing test file...");
                var filePath = Path.Combine(testDir, "test.txt");
                File.WriteAllText(filePath, "This is a test file created by SimpleMcpTest");
                Console.WriteLine($"✓ File written to: {filePath}");

                // Read the file
                Console.WriteLine("\nReading test file...");
                var content = File.ReadAllText(filePath);
                Console.WriteLine($"✓ File content: {content}");

                // Create a directory
                Console.WriteLine("\nCreating test directory...");
                var dirPath = Path.Combine(testDir, "test_dir");
                Directory.CreateDirectory(dirPath);
                Console.WriteLine($"✓ Directory created: {dirPath}");

                // List files
                Console.WriteLine("\nListing files in test directory...");
                var files = Directory.GetFiles(testDir);
                Console.WriteLine($"✓ Found {files.Length} files:");
                foreach (var file in files)
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)}");
                }

                // Delete the file
                Console.WriteLine("\nDeleting test file...");
                File.Delete(filePath);
                Console.WriteLine($"✓ File deleted: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error testing FileSystem operations: {ex.Message}");
            }
        }

        private static async Task TestFetchOperationsAsync()
        {
            Console.WriteLine("\nTesting Fetch Operations:");
            Console.WriteLine("------------------------");

            try
            {
                // Create HttpClient
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Adept/1.0");

                // Fetch a URL
                Console.WriteLine("Fetching example.com...");
                var response = await httpClient.GetAsync("https://www.example.com");
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✓ Fetched {html.Length} bytes from example.com");

                // Extract text (simple version)
                Console.WriteLine("\nExtracting text from HTML...");
                var startIndex = html.IndexOf("<h1>");
                var endIndex = html.IndexOf("</h1>");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    var h1Content = html.Substring(startIndex + 4, endIndex - startIndex - 4);
                    Console.WriteLine($"✓ Extracted h1 content: {h1Content}");
                }
                else
                {
                    Console.WriteLine("✗ Could not extract h1 content");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error testing Fetch operations: {ex.Message}");
            }
        }

        private static async Task TestPuppeteerOperationsAsync(string testDir)
        {
            Console.WriteLine("\nTesting Puppeteer Operations:");
            Console.WriteLine("----------------------------");

            try
            {
                // Download the Chromium browser if not already installed
                Console.WriteLine("Downloading Chromium browser...");
                await new BrowserFetcher().DownloadAsync();

                // Launch the browser
                Console.WriteLine("Launching browser...");
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });

                // Create a new page
                Console.WriteLine("Creating new page...");
                var page = await browser.NewPageAsync();

                // Set viewport
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = 1280,
                    Height = 800
                });

                // Navigate to a URL
                Console.WriteLine("Navigating to example.com...");
                var response = await page.GoToAsync("https://www.example.com", new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Load }
                });

                // Get page title
                var title = await page.GetTitleAsync();
                Console.WriteLine($"✓ Page title: {title}");

                // Take a screenshot
                Console.WriteLine("Taking screenshot...");
                var screenshotPath = Path.Combine(testDir, "screenshot.png");
                await page.ScreenshotAsync(screenshotPath);
                Console.WriteLine($"✓ Screenshot saved to: {screenshotPath}");

                // Extract content
                Console.WriteLine("Extracting content...");
                var content = await page.EvaluateFunctionAsync<string>(@"() => {
                    const element = document.querySelector('h1');
                    return element ? element.textContent : '';
                }");
                Console.WriteLine($"✓ Extracted content: {content}");

                // Close the browser
                Console.WriteLine("Closing browser...");
                await browser.CloseAsync();
                Console.WriteLine("✓ Browser closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error testing Puppeteer operations: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task TestBraveSearchOperationsAsync()
        {
            Console.WriteLine("\nTesting Brave Search Operations:");
            Console.WriteLine("-------------------------------");

            try
            {
                // Create HttpClient
                using var httpClient = new HttpClient();

                // Prompt for the Brave Search API key
                Console.Write("Enter your Brave Search API key: ");
                var apiKey = Console.ReadLine();

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("✗ No API key provided. Skipping Brave Search test.");
                    return;
                }

                Console.WriteLine("API key received. Proceeding with test...");

                // Set up the request headers
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("X-Subscription-Token", apiKey);

                // Perform a search
                Console.WriteLine("Performing search for 'puppeteer browser automation'...");
                var searchUrl = "https://api.search.brave.com/res/v1/web/search?q=puppeteer+browser+automation&count=5";
                var response = await httpClient.GetAsync(searchUrl);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var searchResults = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

                    if (searchResults != null && searchResults.TryGetValue("web", out var webResults) &&
                        webResults.TryGetProperty("results", out var results))
                    {
                        var resultCount = results.GetArrayLength();
                        Console.WriteLine($"✓ Search successful! Found {resultCount} results.");

                        // Display the first few results
                        Console.WriteLine("\nTop search results:");
                        for (int i = 0; i < Math.Min(3, resultCount); i++)
                        {
                            var result = results[i];
                            var title = result.GetProperty("title").GetString();
                            var url = result.GetProperty("url").GetString();
                            Console.WriteLine($"  {i + 1}. {title}");
                            Console.WriteLine($"     {url}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("✗ Could not parse search results.");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Search failed with status code: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"  Error details: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error testing Brave Search operations: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
