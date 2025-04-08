using Adept.Common.Interfaces;
using Adept.Common.Models.FileSystem;
using Adept.Services.Configuration;
using Adept.Services.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileSystemTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Register services
            services.AddSingleton<IDatabaseContext, MockDatabaseContext>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IFileSystemService, ScratchpadService>();
            services.AddSingleton<MarkdownProcessor>();
            services.AddSingleton<FileOrganizer>();
            services.AddSingleton<FileSearchService>();

            var serviceProvider = services.BuildServiceProvider();

            // Get services
            var fileSystemService = serviceProvider.GetRequiredService<IFileSystemService>();
            var markdownProcessor = serviceProvider.GetRequiredService<MarkdownProcessor>();
            var fileOrganizer = serviceProvider.GetRequiredService<FileOrganizer>();
            var fileSearchService = serviceProvider.GetRequiredService<FileSearchService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Test file system operations
                logger.LogInformation("Testing file system operations");
                logger.LogInformation("Scratchpad directory: {ScratchpadDirectory}", fileSystemService.ScratchpadDirectory);

                // Ensure standard folders exist
                await fileSystemService.EnsureStandardFoldersExistAsync();
                logger.LogInformation("Standard folders created");

                // Create a test file
                var testFilePath = "test.md";
                var testContent = @"---
title: Test Markdown File
description: This is a test markdown file
author: ADEPT
date: 2023-06-01
tags: test, markdown, adept
---

# Test Markdown File

This is a test markdown file created by the ADEPT file system test.

## Features

- Markdown parsing
- Metadata extraction
- File organization
";

                var fileInfo = await fileSystemService.WriteFileAsync(testFilePath, testContent);
                logger.LogInformation("Test file created: {FilePath}", fileInfo.FullName);

                // Read the file
                var content = await fileSystemService.ReadFileAsync(testFilePath);
                logger.LogInformation("File content read: {Length} characters", content.Length);

                // Extract metadata
                var metadata = await markdownProcessor.ExtractMetadataAsync(fileInfo.FullName);
                logger.LogInformation("Metadata extracted:");
                logger.LogInformation("  Title: {Title}", metadata.Title);
                logger.LogInformation("  Description: {Description}", metadata.Description);
                logger.LogInformation("  Author: {Author}", metadata.Author);
                logger.LogInformation("  Date: {Date}", metadata.CreationDate);
                logger.LogInformation("  Tags: {Tags}", string.Join(", ", metadata.Tags));

                // Create some additional test files
                await fileSystemService.WriteFileAsync("test1.txt", "This is test file 1");
                await fileSystemService.WriteFileAsync("test2.txt", "This is test file 2");
                await fileSystemService.WriteFileAsync("test3.md", "# Test 3\n\nThis is test file 3");

                // List files
                var (files, directories) = await fileSystemService.ListFilesAsync();
                logger.LogInformation("Files in root directory: {Count}", files.Count());
                foreach (var file in files)
                {
                    logger.LogInformation("  {FileName} - {Size} bytes", file.Name, file.Length);
                }

                // Search for files
                var searchResults = await fileSearchService.SearchFilesByNameAsync("test");
                logger.LogInformation("Search results for 'test': {Count}", searchResults.Count());
                foreach (var file in searchResults)
                {
                    logger.LogInformation("  {FileName}", file.Name);
                }

                // Search for content
                var contentResults = await fileSystemService.SearchFilesByContentAsync("markdown");
                logger.LogInformation("Content search results for 'markdown': {Count}", contentResults.Count());
                foreach (var file in contentResults)
                {
                    logger.LogInformation("  {FileName}", file.Name);
                }

                // Create a directory
                var testDirInfo = await fileSystemService.CreateDirectoryAsync("testdir");
                logger.LogInformation("Test directory created: {DirectoryName}", testDirInfo.Name);

                // Move a file
                var moveResult = await fileSystemService.MoveAsync("test1.txt", "testdir/test1.txt");
                logger.LogInformation("File moved: {Path} ({Type})", moveResult.Path, moveResult.Type);

                // Copy a file
                var copyResult = await fileSystemService.CopyAsync("test2.txt", "testdir/test2_copy.txt");
                logger.LogInformation("File copied: {Path} ({Type})", copyResult.Path, copyResult.Type);

                // Organize files
                var organizeCount = await fileOrganizer.OrganizeFilesByTypeAsync("");
                logger.LogInformation("Files organized: {Count}", organizeCount);

                // Clean up
                await fileSystemService.DeleteAsync("test.md");
                await fileSystemService.DeleteAsync("test2.txt");
                await fileSystemService.DeleteAsync("test3.md");
                await fileSystemService.DeleteAsync("testdir", true);
                logger.LogInformation("Test files cleaned up");

                logger.LogInformation("All tests completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during file system tests");
            }
        }
    }
}
