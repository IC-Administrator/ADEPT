using Adept.Core.Models;
using Adept.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Adept.TestUtilities.Helpers
{
    /// <summary>
    /// Helper class for generating test data
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// Generate a random string of the specified length
        /// </summary>
        /// <param name="length">The length of the string to generate</param>
        /// <returns>A random string</returns>
        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[Random.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        /// Generate a random date within the specified range
        /// </summary>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <returns>A random date within the range</returns>
        public static DateTime GenerateRandomDate(DateTime startDate, DateTime endDate)
        {
            TimeSpan timeSpan = endDate - startDate;
            TimeSpan newSpan = new TimeSpan(0, Random.Next(0, (int)timeSpan.TotalMinutes), 0);
            return startDate + newSpan;
        }

        /// <summary>
        /// Generate a list of random LLM messages for testing
        /// </summary>
        /// <param name="count">The number of messages to generate</param>
        /// <returns>A list of random LLM messages</returns>
        public static List<LlmMessage> GenerateRandomLlmMessages(int count)
        {
            var messages = new List<LlmMessage>();

            // Always add a system message first
            messages.Add(LlmMessage.System("You are a helpful assistant."));

            // Add alternating user and assistant messages
            for (int i = 0; i < count - 1; i++)
            {
                if (i % 2 == 0)
                {
                    messages.Add(LlmMessage.User($"User message {i/2 + 1}: {GenerateRandomString(20)}"));
                }
                else
                {
                    messages.Add(LlmMessage.Assistant($"Assistant response {i/2 + 1}: {GenerateRandomString(30)}"));
                }
            }

            return messages;
        }

        /// <summary>
        /// Generate a random file path
        /// </summary>
        /// <returns>A random file path</returns>
        public static string GenerateRandomFilePath()
        {
            string[] extensions = { ".txt", ".md", ".json", ".csv", ".xml" };
            string fileName = $"{GenerateRandomString(8)}{extensions[Random.Next(extensions.Length)]}";
            string[] directories = { "", "folder1", "folder2/subfolder", "documents" };
            string directory = directories[Random.Next(directories.Length)];

            return string.IsNullOrEmpty(directory) ? fileName : $"{directory}/{fileName}";
        }
    }
}
