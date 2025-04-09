namespace Adept.Services.Tests
{
    /// <summary>
    /// Constants used in services tests
    /// </summary>
    public static class TestConstants
    {
        /// <summary>
        /// Sample LLM provider names
        /// </summary>
        public static class LlmProviders
        {
            public const string OpenAI = "OpenAI";
            public const string Anthropic = "Anthropic";
            public const string Google = "Google";
            public const string Meta = "Meta";
            public const string DeepSeek = "DeepSeek";
            public const string OpenRouter = "OpenRouter";
        }

        /// <summary>
        /// Sample LLM model IDs
        /// </summary>
        public static class LlmModels
        {
            public const string GPT4 = "gpt-4";
            public const string GPT4Turbo = "gpt-4-turbo";
            public const string GPT35Turbo = "gpt-3.5-turbo";
            public const string Claude3Opus = "claude-3-opus-20240229";
            public const string Claude3Sonnet = "claude-3-sonnet-20240229";
            public const string Claude3Haiku = "claude-3-haiku-20240307";
            public const string Gemini = "gemini-1.5-pro";
            public const string Llama3 = "meta/llama-3-70b-instruct";
            public const string DeepSeekCoder = "deepseek-coder";
        }

        /// <summary>
        /// Sample file paths for testing
        /// </summary>
        public static class FilePaths
        {
            public const string TextFile = "test.txt";
            public const string MarkdownFile = "test.md";
            public const string JsonFile = "test.json";
            public const string CsvFile = "test.csv";
            public const string TestDirectory = "test_dir";
        }

        /// <summary>
        /// Sample file contents for testing
        /// </summary>
        public static class FileContents
        {
            public const string TextFileContent = "This is a test file.";
            
            public const string MarkdownFileContent = @"---
title: Test Markdown File
description: This is a test markdown file
author: ADEPT
date: 2023-06-01
tags: test, markdown, adept
---

# Test Markdown File

This is a test markdown file.

## Features
- Feature 1
- Feature 2
- Feature 3
";
            
            public const string JsonFileContent = @"{
  ""name"": ""Test JSON File"",
  ""description"": ""This is a test JSON file"",
  ""properties"": {
    ""property1"": ""value1"",
    ""property2"": ""value2""
  }
}";
            
            public const string CsvFileContent = @"Name,Age,Email
John Doe,30,john@example.com
Jane Smith,25,jane@example.com
Bob Johnson,40,bob@example.com";
        }

        /// <summary>
        /// Sample calendar event data for testing
        /// </summary>
        public static class CalendarEvents
        {
            public const string EventTitle = "Test Event";
            public const string EventDescription = "This is a test event";
            public const string EventLocation = "Test Location";
            public const string EventStartTime = "2023-06-01T10:00:00";
            public const string EventEndTime = "2023-06-01T11:00:00";
        }
    }
}
