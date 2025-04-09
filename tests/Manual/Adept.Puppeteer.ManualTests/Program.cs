using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Adept.Puppeteer.ManualTests
{
    /// <summary>
    /// Manual tests for Puppeteer integration
    /// </summary>
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static ILogger<Program> _logger = null!;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Adept Puppeteer Manual Tests");
            Console.WriteLine("===========================");

            // Initialize services
            InitializeServices();

            // Get services
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

            // Display menu
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nPuppeteer Test Menu:");
                Console.WriteLine("1. Basic Browser Test");
                Console.WriteLine("2. Web Scraping Test");
                Console.WriteLine("3. Form Interaction Test");
                Console.WriteLine("4. PDF Generation Test");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await BasicBrowserTestAsync();
                            break;
                        case "2":
                            await WebScrapingTestAsync();
                            break;
                        case "3":
                            await FormInteractionTestAsync();
                            break;
                        case "4":
                            await PdfGenerationTestAsync();
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

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Basic browser test
        /// </summary>
        private static async Task BasicBrowserTestAsync()
        {
            Console.WriteLine("Basic Browser Test");
            Console.WriteLine("=================\n");

            // Download the browser if needed
            Console.WriteLine("Downloading browser if needed...");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Launch the browser
            Console.WriteLine("Launching browser...");
            var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
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
            }
            finally
            {
                // Close the browser
                Console.WriteLine("\nClosing browser...");
                await browser.CloseAsync();
            }
        }

        /// <summary>
        /// Web scraping test
        /// </summary>
        private static async Task WebScrapingTestAsync()
        {
            Console.WriteLine("Web Scraping Test");
            Console.WriteLine("================\n");

            // Ask for a URL to scrape
            Console.Write("Enter a URL to scrape (default: https://news.ycombinator.com): ");
            string url = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://news.ycombinator.com";
            }

            // Download the browser if needed
            Console.WriteLine("Downloading browser if needed...");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Launch the browser
            Console.WriteLine("Launching browser...");
            var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            try
            {
                // Create a new page
                Console.WriteLine("Creating new page...");
                var page = await browser.NewPageAsync();

                // Navigate to the URL
                Console.WriteLine($"Navigating to {url}...");
                await page.GoToAsync(url);

                // Wait for the content to load
                await page.WaitForSelectorAsync("body");

                // Extract information based on the URL
                Console.WriteLine("\nExtracting information...");

                if (url.Contains("news.ycombinator.com"))
                {
                    // Extract Hacker News stories
                    var stories = await page.EvaluateExpressionAsync<string[]>(@"
                        Array.from(document.querySelectorAll('.titleline > a')).map(a => {
                            return a.innerText;
                        })
                    ");

                    var points = await page.EvaluateExpressionAsync<string[]>(@"
                        Array.from(document.querySelectorAll('.score')).map(span => {
                            return span.innerText;
                        })
                    ");

                    Console.WriteLine($"\nFound {stories.Length} stories:");
                    for (int i = 0; i < Math.Min(10, stories.Length); i++)
                    {
                        string pointsText = i < points.Length ? points[i] : "unknown points";
                        Console.WriteLine($"{i + 1}. {stories[i]} ({pointsText})");
                    }
                }
                else
                {
                    // Generic scraping
                    var links = await page.EvaluateExpressionAsync<string[]>(@"
                        Array.from(document.querySelectorAll('a')).map(a => {
                            return a.href;
                        })
                    ");

                    var headings = await page.EvaluateExpressionAsync<string[]>(@"
                        Array.from(document.querySelectorAll('h1, h2, h3')).map(h => {
                            return h.innerText;
                        })
                    ");

                    Console.WriteLine($"\nFound {links.Length} links:");
                    for (int i = 0; i < Math.Min(10, links.Length); i++)
                    {
                        Console.WriteLine($"- {links[i]}");
                    }

                    Console.WriteLine($"\nFound {headings.Length} headings:");
                    for (int i = 0; i < Math.Min(10, headings.Length); i++)
                    {
                        Console.WriteLine($"- {headings[i]}");
                    }
                }
            }
            finally
            {
                // Close the browser
                Console.WriteLine("\nClosing browser...");
                await browser.CloseAsync();
            }
        }

        /// <summary>
        /// Form interaction test
        /// </summary>
        private static async Task FormInteractionTestAsync()
        {
            Console.WriteLine("Form Interaction Test");
            Console.WriteLine("====================\n");

            // Download the browser if needed
            Console.WriteLine("Downloading browser if needed...");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Launch the browser
            Console.WriteLine("Launching browser...");
            var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
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

                // Navigate to a website with a form
                string url = "https://www.google.com";
                Console.WriteLine($"Navigating to {url}...");
                await page.GoToAsync(url);

                // Wait for the search input to be available
                await page.WaitForSelectorAsync("input[name=q]");

                // Type in the search box
                Console.WriteLine("Typing in search box...");
                await page.TypeAsync("input[name=q]", "Puppeteer C# examples");

                // Wait a moment for the user to see the typing
                await Task.Delay(1000);

                // Submit the form
                Console.WriteLine("Submitting search...");
                await page.Keyboard.PressAsync("Enter");

                // Wait for navigation to complete
                await page.WaitForNavigationAsync();

                // Extract search results
                Console.WriteLine("\nExtracting search results...");
                var searchResults = await page.EvaluateExpressionAsync<string[]>(@"
                    Array.from(document.querySelectorAll('h3')).map(h => {
                        return h.innerText;
                    })
                ");

                Console.WriteLine($"\nFound {searchResults.Length} search results:");
                for (int i = 0; i < Math.Min(10, searchResults.Length); i++)
                {
                    Console.WriteLine($"{i + 1}. {searchResults[i]}");
                }

                // Click on the first result
                Console.WriteLine("\nClicking on the first result...");
                await page.ClickAsync("h3");

                // Wait for navigation to complete
                await page.WaitForNavigationAsync();

                // Get the page title
                string title = await page.GetTitleAsync();
                Console.WriteLine($"Navigated to: {title}");
                Console.WriteLine($"URL: {page.Url}");
            }
            finally
            {
                // Close the browser
                Console.WriteLine("\nClosing browser...");
                await browser.CloseAsync();
            }
        }

        /// <summary>
        /// PDF generation test
        /// </summary>
        private static async Task PdfGenerationTestAsync()
        {
            Console.WriteLine("PDF Generation Test");
            Console.WriteLine("==================\n");

            // Ask for a URL to convert to PDF
            Console.Write("Enter a URL to convert to PDF (default: https://www.example.com): ");
            string url = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://www.example.com";
            }

            // Download the browser if needed
            Console.WriteLine("Downloading browser if needed...");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Launch the browser
            Console.WriteLine("Launching browser...");
            var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });

            try
            {
                // Create a new page
                Console.WriteLine("Creating new page...");
                var page = await browser.NewPageAsync();

                // Navigate to the URL
                Console.WriteLine($"Navigating to {url}...");
                await page.GoToAsync(url, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.NetworkIdle0 }
                });

                // Generate PDF
                string pdfPath = Path.Combine(Path.GetTempPath(), "puppeteer_output.pdf");
                Console.WriteLine($"Generating PDF: {pdfPath}");
                await page.PdfAsync(new PdfOptions
                {
                    Path = pdfPath,
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                    MarginOptions = new MarginOptions
                    {
                        Top = "1cm",
                        Right = "1cm",
                        Bottom = "1cm",
                        Left = "1cm"
                    }
                });

                Console.WriteLine($"PDF generated successfully: {pdfPath}");
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
