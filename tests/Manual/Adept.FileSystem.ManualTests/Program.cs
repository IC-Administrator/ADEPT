using Adept.Core.Interfaces;
using Adept.Services.FileSystem;
using Adept.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Adept.FileSystem.ManualTests
{
    /// <summary>
    /// Manual tests for the file system functionality
    /// </summary>
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static ILogger<Program> _logger = null!;
        private static IFileSystemService _fileSystemService = null!;
        private static MarkdownProcessor _markdownProcessor = null!;
        private static FileOrganizer _fileOrganizer = null!;
        private static FileSearchService _fileSearchService = null!;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Adept File System Manual Tests");
            Console.WriteLine("==============================");

            // Initialize services
            InitializeServices();

            // Get services
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            _fileSystemService = _serviceProvider.GetRequiredService<IFileSystemService>();
            _markdownProcessor = _serviceProvider.GetRequiredService<MarkdownProcessor>();
            _fileOrganizer = _serviceProvider.GetRequiredService<FileOrganizer>();
            _fileSearchService = _serviceProvider.GetRequiredService<FileSearchService>();

            // Display menu
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nFile System Test Menu:");
                Console.WriteLine("1. Test Basic File Operations");
                Console.WriteLine("2. Test Markdown Processing");
                Console.WriteLine("3. Test File Organization");
                Console.WriteLine("4. Test File Search");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await TestBasicFileOperationsAsync();
                            break;
                        case "2":
                            await TestMarkdownProcessingAsync();
                            break;
                        case "3":
                            await TestFileOrganizationAsync();
                            break;
                        case "4":
                            await TestFileSearchAsync();
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

            // Create a test directory in the temp folder
            var testDirectory = Path.Combine(
                Path.GetTempPath(),
                "adept_fs_manual_test");

            // Ensure the directory exists
            Directory.CreateDirectory(testDirectory);
            Console.WriteLine($"Test directory: {testDirectory}");

            // Add file system services
            services.AddSingleton<IFileSystemService>(provider =>
                new FileSystemService(
                    provider.GetRequiredService<ILogger<FileSystemService>>(),
                    testDirectory));

            services.AddSingleton<MarkdownProcessor>();
            services.AddSingleton<FileOrganizer>();
            services.AddSingleton<FileSearchService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Test basic file operations
        /// </summary>
        private static async Task TestBasicFileOperationsAsync()
        {
            Console.WriteLine("Testing Basic File Operations");
            Console.WriteLine("-----------------------------");

            // Ensure standard folders exist
            await _fileSystemService.EnsureStandardFoldersExistAsync();
            Console.WriteLine("Standard folders created");

            // Create a test file
            var testFilePath = "test.txt";
            var testContent = "This is a test file created by the manual test program.";
            await _fileSystemService.WriteFileAsync(testFilePath, testContent);
            Console.WriteLine($"Test file created: {testFilePath}");

            // Read the test file
            var content = await _fileSystemService.ReadFileAsync(testFilePath);
            Console.WriteLine($"File content: {content}");

            // Create a directory
            var testDirPath = "test_dir";
            var dirInfo = await _fileSystemService.CreateDirectoryAsync(testDirPath);
            Console.WriteLine($"Test directory created: {dirInfo.Name}");

            // Move the file
            var movedFilePath = Path.Combine(testDirPath, "moved_test.txt");
            var moveResult = await _fileSystemService.MoveAsync(testFilePath, movedFilePath);
            Console.WriteLine($"File moved: {moveResult.Path}");

            // Copy the file
            var copiedFilePath = "copied_test.txt";
            var copyResult = await _fileSystemService.CopyAsync(movedFilePath, copiedFilePath);
            Console.WriteLine($"File copied: {copyResult.Path}");

            // List files
            var (files, directories) = await _fileSystemService.ListFilesAsync();
            Console.WriteLine("\nFiles in root directory:");
            foreach (var file in files)
            {
                Console.WriteLine($"  {file.Name} - {file.Length} bytes");
            }

            Console.WriteLine("\nDirectories in root directory:");
            foreach (var dir in directories)
            {
                Console.WriteLine($"  {dir.Name}");
            }

            // Delete the copied file
            var deleteResult = await _fileSystemService.DeleteAsync(copiedFilePath);
            Console.WriteLine($"File deleted: {deleteResult}");
        }

        /// <summary>
        /// Test markdown processing
        /// </summary>
        private static async Task TestMarkdownProcessingAsync()
        {
            Console.WriteLine("Testing Markdown Processing");
            Console.WriteLine("--------------------------");

            // Create a markdown file
            var markdownFilePath = "test.md";
            var markdownContent = @"---
title: Test Markdown File
description: This is a test markdown file
author: ADEPT
date: 2023-06-01
tags: test, markdown, adept
---

# Test Markdown File

This is a test markdown file created by the manual test program.

## Features
- Feature 1
- Feature 2
- Feature 3

## Code Example
```csharp
public class Example
{
    public void DoSomething()
    {
        Console.WriteLine(""Hello, world!"");
    }
}
```
";

            await _fileSystemService.WriteFileAsync(markdownFilePath, markdownContent);
            Console.WriteLine($"Markdown file created: {markdownFilePath}");

            // Extract front matter
            var frontMatter = _markdownProcessor.ExtractFrontMatter(markdownContent);
            Console.WriteLine("\nFront Matter:");
            foreach (var kvp in frontMatter)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }

            // Extract markdown content
            var extractedContent = _markdownProcessor.ExtractMarkdownContent(markdownContent);
            Console.WriteLine("\nMarkdown Content (first 100 chars):");
            Console.WriteLine($"  {extractedContent.Substring(0, Math.Min(100, extractedContent.Length))}...");

            // Convert to HTML
            var html = _markdownProcessor.ConvertMarkdownToHtml(extractedContent);
            Console.WriteLine("\nHTML (first 100 chars):");
            Console.WriteLine($"  {html.Substring(0, Math.Min(100, html.Length))}...");

            // Extract headings
            var headings = _markdownProcessor.ExtractHeadings(extractedContent);
            Console.WriteLine("\nHeadings:");
            foreach (var heading in headings)
            {
                Console.WriteLine($"  {new string('#', heading.Level)} {heading.Text}");
            }
        }

        /// <summary>
        /// Test file organization
        /// </summary>
        private static async Task TestFileOrganizationAsync()
        {
            Console.WriteLine("Testing File Organization");
            Console.WriteLine("-------------------------");

            // Create test files of different types
            await _fileSystemService.WriteFileAsync("document1.txt", "This is a text document.");
            await _fileSystemService.WriteFileAsync("document2.md", "# This is a markdown document");
            await _fileSystemService.WriteFileAsync("data1.json", "{ \"name\": \"Test JSON\" }");
            await _fileSystemService.WriteFileAsync("data2.csv", "Name,Age,Email");
            await _fileSystemService.WriteFileAsync("code1.cs", "public class Test {}");
            await _fileSystemService.WriteFileAsync("code2.py", "def test(): pass");

            Console.WriteLine("Test files created");

            // List files before organization
            var (files, _) = await _fileSystemService.ListFilesAsync();
            Console.WriteLine("\nFiles before organization:");
            foreach (var file in files)
            {
                Console.WriteLine($"  {file.Name}");
            }

            // Organize files by type
            var organizedCount = await _fileOrganizer.OrganizeFilesByTypeAsync("");
            Console.WriteLine($"\nFiles organized: {organizedCount}");

            // List directories after organization
            var (_, directories) = await _fileSystemService.ListFilesAsync();
            Console.WriteLine("\nDirectories after organization:");
            foreach (var dir in directories)
            {
                Console.WriteLine($"  {dir.Name}");
                var (dirFiles, _) = await _fileSystemService.ListFilesAsync(dir.Name);
                foreach (var file in dirFiles)
                {
                    Console.WriteLine($"    {file.Name}");
                }
            }
        }

        /// <summary>
        /// Test file search
        /// </summary>
        private static async Task TestFileSearchAsync()
        {
            Console.WriteLine("Testing File Search");
            Console.WriteLine("------------------");

            // Create test files with different content
            await _fileSystemService.WriteFileAsync("search_test1.txt", "This file contains the word apple.");
            await _fileSystemService.WriteFileAsync("search_test2.txt", "This file contains the words apple and banana.");
            await _fileSystemService.WriteFileAsync("search_test3.txt", "This file contains the word banana but not apple.");
            await _fileSystemService.WriteFileAsync("search_test4.md", "# Search Test\nThis markdown file contains the word apple.");

            Console.WriteLine("Test files created");

            // Search by name
            Console.WriteLine("\nSearching for files with 'test' in the name:");
            var nameResults = await _fileSearchService.SearchFilesByNameAsync("test");
            foreach (var file in nameResults)
            {
                Console.WriteLine($"  {file.Name}");
            }

            // Search by content
            Console.WriteLine("\nSearching for files containing 'apple':");
            var contentResults = await _fileSearchService.SearchFilesByContentAsync("apple");
            foreach (var file in contentResults)
            {
                Console.WriteLine($"  {file.Name}");
            }

            // Search by extension
            Console.WriteLine("\nSearching for .md files:");
            var extensionResults = await _fileSearchService.SearchFilesByExtensionAsync(".md");
            foreach (var file in extensionResults)
            {
                Console.WriteLine($"  {file.Name}");
            }

            // Combined search
            Console.WriteLine("\nSearching for .txt files containing 'banana':");
            var combinedResults = await _fileSearchService.SearchFilesAsync(
                namePattern: null,
                contentPattern: "banana",
                extension: ".txt");
            foreach (var file in combinedResults)
            {
                Console.WriteLine($"  {file.Name}");
            }
        }
    }
}
