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
    /// Tests for the JsonHelper class
    /// </summary>
    public class JsonHelperTests : IClassFixture<JsonFixture>
    {
        private readonly JsonFixture _fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonHelperTests"/> class
        /// </summary>
        /// <param name="fixture">The JSON fixture</param>
        public JsonHelperTests(JsonFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>
        /// Tests that Serialize serializes an object to JSON
        /// </summary>
        [Fact]
        public void Serialize_ValidObject_ReturnsJsonString()
        {
            // Arrange
            var obj = new { Name = "Test", Value = 42 };

            // Act
            var result = JsonHelper.Serialize(obj);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"Name\":\"Test\"", result);
            Assert.Contains("\"Value\":42", result);
        }

        /// <summary>
        /// Tests that Serialize throws an exception when given a null object
        /// </summary>
        [Fact]
        public void Serialize_NullObject_ThrowsArgumentNullException()
        {
            // Act & Assert
            AssertExtensions.ThrowsWithMessage<ArgumentNullException>(
                () => JsonHelper.Serialize<object>(null),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that Serialize with options serializes an object to JSON using the specified options
        /// </summary>
        [Fact]
        public void Serialize_WithOptions_UsesSpecifiedOptions()
        {
            // Arrange
            var obj = new { Name = "Test", Value = 42 };
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Act
            var result = JsonHelper.Serialize(obj, options);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"Name\": \"Test\"", result);
            Assert.Contains("\"Value\": 42", result);
            Assert.Contains("\n", result);
        }

        /// <summary>
        /// Tests that Deserialize deserializes a JSON string to an object
        /// </summary>
        [Fact]
        public void Deserialize_ValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"Value\":42}";

            // Act
            var result = JsonHelper.Deserialize<TestObject>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Tests that Deserialize throws an exception when given a null or empty JSON string
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deserialize_NullOrEmptyJson_ThrowsArgumentException(string json)
        {
            // Act & Assert
            AssertExtensions.ThrowsWithMessage<ArgumentException>(
                () => JsonHelper.Deserialize<TestObject>(json),
                "Value cannot be null or empty");
        }

        /// <summary>
        /// Tests that Deserialize throws an exception when given an invalid JSON string
        /// </summary>
        [Fact]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var invalidJson = _fixture.InvalidJson;

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonHelper.Deserialize<TestObject>(invalidJson));
        }

        /// <summary>
        /// Tests that Deserialize with options deserializes a JSON string to an object using the specified options
        /// </summary>
        [Fact]
        public void Deserialize_WithOptions_UsesSpecifiedOptions()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Act
            var result = JsonHelper.Deserialize<TestObject>(json, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Tests that ValidateJson returns true for valid JSON
        /// </summary>
        [Fact]
        public void ValidateJson_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = _fixture.SimpleJson;

            // Act
            var result = JsonHelper.ValidateJson(json, out var error);

            // Assert
            Assert.True(result);
            Assert.Null(error);
        }

        /// <summary>
        /// Tests that ValidateJson returns false for invalid JSON
        /// </summary>
        [Fact]
        public void ValidateJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var invalidJson = _fixture.InvalidJson;

            // Act
            var result = JsonHelper.ValidateJson(invalidJson, out var error);

            // Assert
            Assert.False(result);
            Assert.NotNull(error);
        }

        /// <summary>
        /// Tests that ValidateJson returns false for null or empty JSON
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateJson_NullOrEmptyJson_ReturnsFalse(string json)
        {
            // Act
            var result = JsonHelper.ValidateJson(json, out var error);

            // Assert
            Assert.False(result);
            Assert.NotNull(error);
        }

        /// <summary>
        /// Tests that ValidateJson<T> returns true for valid JSON that can be deserialized to the specified type
        /// </summary>
        [Fact]
        public void ValidateJsonT_ValidJsonForType_ReturnsTrue()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"Value\":42}";

            // Act
            var result = JsonHelper.ValidateJson<TestObject>(json, out var error);

            // Assert
            Assert.True(result);
            Assert.Null(error);
        }

        /// <summary>
        /// Tests that ValidateJson<T> returns false for JSON that cannot be deserialized to the specified type
        /// </summary>
        [Fact]
        public void ValidateJsonT_InvalidJsonForType_ReturnsFalse()
        {
            // Arrange
            var json = "{\"Name\":\"Test\",\"InvalidProperty\":42}";

            // Act
            var result = JsonHelper.ValidateJson<TestObject>(json, out var error);

            // Assert
            Assert.False(result);
            Assert.NotNull(error);
        }

        /// <summary>
        /// Tests that ValidateJson<T> returns false for null or empty JSON
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateJsonT_NullOrEmptyJson_ReturnsFalse(string json)
        {
            // Act
            var result = JsonHelper.ValidateJson<TestObject>(json, out var error);

            // Assert
            Assert.False(result);
            Assert.NotNull(error);
        }

        /// <summary>
        /// Tests that SerializeIndented serializes an object to indented JSON
        /// </summary>
        [Fact]
        public void SerializeIndented_ValidObject_ReturnsIndentedJsonString()
        {
            // Arrange
            var obj = new { Name = "Test", Value = 42 };

            // Act
            var result = JsonHelper.SerializeIndented(obj);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"Name\": \"Test\"", result);
            Assert.Contains("\"Value\": 42", result);
            Assert.Contains("\n", result);
        }

        /// <summary>
        /// Tests that SerializeIndented throws an exception when given a null object
        /// </summary>
        [Fact]
        public void SerializeIndented_NullObject_ThrowsArgumentNullException()
        {
            // Act & Assert
            AssertExtensions.ThrowsWithMessage<ArgumentNullException>(
                () => JsonHelper.SerializeIndented<object>(null),
                "Value cannot be null");
        }

        /// <summary>
        /// Tests that DeserializeWithCamelCase deserializes a JSON string with camel case property names
        /// </summary>
        [Fact]
        public void DeserializeWithCamelCase_ValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"name\":\"Test\",\"value\":42}";

            // Act
            var result = JsonHelper.DeserializeWithCamelCase<TestObject>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Value);
        }

        /// <summary>
        /// Tests that DeserializeWithCamelCase throws an exception when given a null or empty JSON string
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void DeserializeWithCamelCase_NullOrEmptyJson_ThrowsArgumentException(string json)
        {
            // Act & Assert
            AssertExtensions.ThrowsWithMessage<ArgumentException>(
                () => JsonHelper.DeserializeWithCamelCase<TestObject>(json),
                "Value cannot be null or empty");
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
