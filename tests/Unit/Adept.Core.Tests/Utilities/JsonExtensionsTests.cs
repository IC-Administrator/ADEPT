using Adept.Common.Json;
using Adept.Core.Models;
using Adept.TestUtilities.Fixtures;
using Adept.TestUtilities.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Adept.Core.Tests.Utilities
{
    /// <summary>
    /// Tests for the JsonExtensions class
    /// </summary>
    public class JsonExtensionsTests : IClassFixture<JsonFixture>
    {
        private readonly JsonFixture _fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonExtensionsTests"/> class
        /// </summary>
        /// <param name="fixture">The JSON fixture</param>
        public JsonExtensionsTests(JsonFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Tests that ToJson serializes an object to JSON
        /// </summary>
        [Fact]
        public void ToJson_ValidObject_ReturnsJsonString()
        {
            // Arrange
            var obj = new TestObject { Name = "Test", Value = 42 };

            // Act
            var result = obj.ToJson();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"Name\":\"Test\"", result);
            Assert.Contains("\"Value\":42", result);
        }

        /// <summary>
        /// Tests that ToJson throws an exception when called on a null object
        /// </summary>
        [Fact]
        public void ToJson_NullObject_ThrowsNullReferenceException()
        {
            // Arrange
            TestObject obj = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => obj.ToJson());
        }

        /// <summary>
        /// Tests that ToJson with options serializes an object to JSON using the specified options
        /// </summary>
        [Fact]
        public void ToJson_WithOptions_UsesSpecifiedOptions()
        {
            // Arrange
            var obj = new TestObject { Name = "Test", Value = 42 };
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Act
            var result = obj.ToJson(options);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"Name\": \"Test\"", result);
            Assert.Contains("\"Value\": 42", result);
            Assert.Contains("\n", result);
        }

        /// <summary>
        /// Tests that ToIndentedJson serializes an object to indented JSON
        /// </summary>
        [Fact]
        public void ToIndentedJson_ValidObject_ReturnsIndentedJsonString()
        {
            // Arrange
            var obj = new TestObject { Name = "Test", Value = 42 };

            // Act
            var result = obj.ToIndentedJson();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"Name\": \"Test\"", result);
            Assert.Contains("\"Value\": 42", result);
            Assert.Contains("\n", result);
        }

        /// <summary>
        /// Tests that ToIndentedJson throws an exception when called on a null object
        /// </summary>
        [Fact]
        public void ToIndentedJson_NullObject_ThrowsNullReferenceException()
        {
            // Arrange
            TestObject obj = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => obj.ToIndentedJson());
        }

        /// <summary>
        /// Tests that FromJson deserializes a JSON string to an object
        /// </summary>
        [Fact]
        public void FromJson_ValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"Value\":42}";

            // Act
            var result = json.FromJson<TestObject>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Tests that FromJson throws an exception when called on a null or empty JSON string
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FromJson_NullOrEmptyJson_ThrowsArgumentException(string json)
        {
            // Act & Assert
            AssertExtensions.ThrowsWithMessage<ArgumentException>(
                () => json.FromJson<TestObject>(),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that FromJson throws an exception when called on an invalid JSON string
        /// </summary>
        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var invalidJson = _fixture.InvalidJson;

            // Act & Assert
            Assert.Throws<JsonException>(() => invalidJson.FromJson<TestObject>());
        }

        /// <summary>
        /// Tests that FromJson with options deserializes a JSON string to an object using the specified options
        /// </summary>
        [Fact]
        public void FromJson_WithOptions_UsesSpecifiedOptions()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Act
            var result = json.FromJson<TestObject>(options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Tests that FromJsonWithCamelCase deserializes a JSON string with camel case property names
        /// </summary>
        [Fact]
        public void FromJsonWithCamelCase_ValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";

            // Act
            var result = json.FromJsonWithCamelCase<TestObject>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Tests that FromJsonWithCamelCase throws an exception when called on a null or empty JSON string
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FromJsonWithCamelCase_NullOrEmptyJson_ThrowsArgumentException(string json)
        {
            // Act & Assert
            AssertExtensions.ThrowsWithMessage<ArgumentException>(
                () => json.FromJsonWithCamelCase<TestObject>(),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that IsValidJson returns true for valid JSON
        /// </summary>
        [Fact]
        public void IsValidJson_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = _fixture.SimpleJson;

            // Act
            var result = json.IsValidJson();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that IsValidJson returns false for invalid JSON
        /// </summary>
        [Fact]
        public void IsValidJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var invalidJson = _fixture.InvalidJson;

            // Act
            var result = invalidJson.IsValidJson();

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that IsValidJson returns false for null or empty JSON
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidJson_NullOrEmptyJson_ReturnsFalse(string json)
        {
            // Act
            var result = json.IsValidJson();

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that IsValidJson<T> returns true for valid JSON that can be deserialized to the specified type
        /// </summary>
        [Fact]
        public void IsValidJsonT_ValidJsonForType_ReturnsTrue()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"Value\":42}";

            // Act
            var result = json.IsValidJson<TestObject>();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that IsValidJson<T> returns false for JSON that cannot be deserialized to the specified type
        /// </summary>
        [Fact]
        public void IsValidJsonT_InvalidJsonForType_ReturnsFalse()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"InvalidProperty\":42}";

            // Act
            var result = json.IsValidJson<TestObject>();

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that IsValidJson<T> returns false for null or empty JSON
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidJsonT_NullOrEmptyJson_ReturnsFalse(string json)
        {
            // Act
            var result = json.IsValidJson<TestObject>();

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that Clone creates a deep copy of an object
        /// </summary>
        [Fact]
        public void Clone_ValidObject_ReturnsDeepCopy()
        {
            // Arrange
            var original = new TestObject { Name = "Test", Value = 42 };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotNull(clone);
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Value, clone.Value);
            Assert.NotSame(original, clone);
        }

        /// <summary>
        /// Tests that Clone throws an exception when called on a null object
        /// </summary>
        [Fact]
        public void Clone_NullObject_ThrowsNullReferenceException()
        {
            // Arrange
            TestObject obj = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => obj.Clone());
        }

        /// <summary>
        /// Test class for JSON serialization tests
        /// </summary>
        private class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}
