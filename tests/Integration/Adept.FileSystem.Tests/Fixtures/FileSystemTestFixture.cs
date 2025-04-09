using Adept.Core.Interfaces;
using Adept.Services.FileSystem;
using Adept.TestUtilities.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Adept.FileSystem.Tests.Fixtures
{
    /// <summary>
    /// Fixture for file system integration tests
    /// </summary>
    public class FileSystemTestFixture : IAsyncLifetime, IDisposable
    {
        public string TestDirectory { get; }
        public IServiceProvider ServiceProvider { get; }
        public IFileSystemService FileSystemService => ServiceProvider.GetRequiredService<IFileSystemService>();
        public MarkdownProcessor MarkdownProcessor => ServiceProvider.GetRequiredService<MarkdownProcessor>();
        public FileOrganizer FileOrganizer => ServiceProvider.GetRequiredService<FileOrganizer>();
        public FileSearchService FileSearchService => ServiceProvider.GetRequiredService<FileSearchService>();

        public FileSystemTestFixture()
        {
            // Create a unique test directory
            TestDirectory = Path.Combine(
                Path.GetTempPath(), 
                $"adept_fs_test_{Guid.NewGuid():N}");
            
            // Ensure the directory exists
            Directory.CreateDirectory(TestDirectory);
            
            // Set up services
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Add file system services
            services.AddSingleton<IFileSystemService>(provider => 
                new FileSystemService(
                    provider.GetRequiredService<ILogger<FileSystemService>>(),
                    TestDirectory));
            
            services.AddSingleton<MarkdownProcessor>();
            services.AddSingleton<FileOrganizer>();
            services.AddSingleton<FileSearchService>();
            
            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Initialize the test environment
        /// </summary>
        public async Task InitializeAsync()
        {
            // Create standard folders
            await FileSystemService.EnsureStandardFoldersExistAsync();
            
            // Create some test files
            await CreateTestFilesAsync();
        }

        /// <summary>
        /// Create test files for the tests
        /// </summary>
        private async Task CreateTestFilesAsync()
        {
            // Create a markdown file
            await FileSystemService.WriteFileAsync("test.md", @"---
title: Test Markdown File
description: This is a test markdown file
author: ADEPT
date: 2023-06-01
tags: test, markdown, adept
---

# Test Markdown File

This is a test markdown file created for integration tests.

## Features
- Feature 1
- Feature 2
- Feature 3
");

            // Create a text file
            await FileSystemService.WriteFileAsync("test.txt", "This is a test text file.");
            
            // Create a JSON file
            await FileSystemService.WriteFileAsync("test.json", @"{
  ""name"": ""Test JSON File"",
  ""description"": ""This is a test JSON file"",
  ""properties"": {
    ""property1"": ""value1"",
    ""property2"": ""value2""
  }
}");
        }

        /// <summary>
        /// Cleanup after tests
        /// </summary>
        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, true);
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
            
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
