using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Adept.TestUtilities.Helpers
{
    /// <summary>
    /// Extension methods for assertions
    /// </summary>
    public static class AssertExtensions
    {
        /// <summary>
        /// Assert that a file exists
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        public static void FileExists(string filePath)
        {
            Assert.True(File.Exists(filePath), $"File does not exist: {filePath}");
        }

        /// <summary>
        /// Assert that a directory exists
        /// </summary>
        /// <param name="directoryPath">The path to the directory</param>
        public static void DirectoryExists(string directoryPath)
        {
            Assert.True(Directory.Exists(directoryPath), $"Directory does not exist: {directoryPath}");
        }

        /// <summary>
        /// Assert that a file contains the specified text
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="expectedText">The text that the file should contain</param>
        public static void FileContains(string filePath, string expectedText)
        {
            FileExists(filePath);
            string content = File.ReadAllText(filePath);
            Assert.Contains(expectedText, content);
        }

        /// <summary>
        /// Assert that a collection contains items matching the specified predicate
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <param name="predicate">The predicate to match items against</param>
        /// <param name="expectedCount">The expected number of matching items</param>
        public static void CollectionContains<T>(IEnumerable<T> collection, Func<T, bool> predicate, int expectedCount)
        {
            int actualCount = collection.Count(predicate);
            Assert.Equal(expectedCount, actualCount);
        }

        /// <summary>
        /// Assert that a string contains all the specified substrings
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <param name="substrings">The substrings that should be contained in the string</param>
        public static void StringContainsAll(string value, params string[] substrings)
        {
            foreach (string substring in substrings)
            {
                Assert.Contains(substring, value);
            }
        }

        /// <summary>
        /// Assert that a string does not contain any of the specified substrings
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <param name="substrings">The substrings that should not be contained in the string</param>
        public static void StringDoesNotContainAny(string value, params string[] substrings)
        {
            foreach (string substring in substrings)
            {
                Assert.DoesNotContain(substring, value);
            }
        }

        /// <summary>
        /// Assert that an exception of the specified type is thrown when the action is executed
        /// </summary>
        /// <typeparam name="TException">The type of exception expected</typeparam>
        /// <param name="action">The action that should throw the exception</param>
        /// <param name="messageContains">Optional substring that the exception message should contain</param>
        /// <returns>The thrown exception</returns>
        public static TException ThrowsWithMessage<TException>(Action action, string? messageContains = null)
            where TException : Exception
        {
            TException exception = Assert.Throws<TException>(action);

            if (!string.IsNullOrEmpty(messageContains))
            {
                Assert.Contains(messageContains, exception.Message);
            }

            return exception;
        }
    }
}
