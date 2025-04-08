using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using System.Threading.Tasks;

namespace PuppeteerTest
{
    class SimplePuppeteerTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Simple Puppeteer Test...");

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
                Console.WriteLine($"Page title: {title}");
                
                // Take a screenshot
                Console.WriteLine("Taking screenshot...");
                await page.ScreenshotAsync("example_screenshot.png");
                Console.WriteLine("Screenshot saved to example_screenshot.png");
                
                // Extract content
                Console.WriteLine("Extracting content...");
                var content = await page.EvaluateFunctionAsync<string>(@"() => {
                    const element = document.querySelector('h1');
                    return element ? element.textContent : '';
                }");
                Console.WriteLine($"Extracted content: {content}");
                
                // Close the browser
                Console.WriteLine("Closing browser...");
                await browser.CloseAsync();
                
                Console.WriteLine("Test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
