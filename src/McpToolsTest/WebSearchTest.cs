using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace McpToolsTest
{
    class WebSearchTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Web Search Test");
            Console.WriteLine("==============");

            // Test direct Brave Search API
            await TestBraveSearchApiAsync();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestBraveSearchApiAsync()
        {
            Console.WriteLine("\nTesting Brave Search API directly:");
            Console.WriteLine("----------------------------------");

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

                // Prompt for search query
                Console.Write("Enter search query (or press Enter for default 'puppeteer browser automation'): ");
                var searchQuery = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    searchQuery = "puppeteer browser automation";
                }

                // Perform a search
                Console.WriteLine($"Performing search for '{searchQuery}'...");
                var encodedQuery = Uri.EscapeDataString(searchQuery);
                var searchUrl = $"https://api.search.brave.com/res/v1/web/search?q={encodedQuery}&count=5";
                var response = await httpClient.GetAsync(searchUrl);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    
                    // Save the raw JSON response to a file for inspection
                    var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "brave_search_response.json");
                    File.WriteAllText(jsonFilePath, jsonContent);
                    Console.WriteLine($"✓ Raw JSON response saved to: {jsonFilePath}");
                    
                    var searchResults = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

                    if (searchResults != null && searchResults.TryGetValue("web", out var webResults) &&
                        webResults.TryGetProperty("results", out var results))
                    {
                        var resultCount = results.GetArrayLength();
                        Console.WriteLine($"✓ Search successful! Found {resultCount} results.");

                        // Display the first few results
                        Console.WriteLine("\nTop search results:");
                        for (int i = 0; i < Math.Min(5, resultCount); i++)
                        {
                            var result = results[i];
                            var title = result.GetProperty("title").GetString();
                            var url = result.GetProperty("url").GetString();
                            var description = result.TryGetProperty("description", out var desc) ? desc.GetString() : "No description available";
                            
                            Console.WriteLine($"  {i + 1}. {title}");
                            Console.WriteLine($"     URL: {url}");
                            Console.WriteLine($"     Description: {description}");
                            Console.WriteLine();
                        }
                        
                        // Check if there are any related queries
                        if (searchResults.TryGetValue("query", out var queryInfo) && 
                            queryInfo.TryGetProperty("related", out var relatedQueries))
                        {
                            var relatedCount = relatedQueries.GetArrayLength();
                            if (relatedCount > 0)
                            {
                                Console.WriteLine("\nRelated search queries:");
                                for (int i = 0; i < Math.Min(5, relatedCount); i++)
                                {
                                    var related = relatedQueries[i].GetString();
                                    Console.WriteLine($"  • {related}");
                                }
                            }
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
                Console.WriteLine($"✗ Error testing Brave Search API: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
