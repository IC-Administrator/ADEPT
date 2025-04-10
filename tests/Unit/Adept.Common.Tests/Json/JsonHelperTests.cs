using Adept.Common.Json;
using System.Text.Json;
using Xunit;

namespace Adept.Common.Tests.Json
{
    public class JsonHelperTests
    {
        [Fact]
        public void Serialize_ValidObject_ReturnsJsonString()
        {
            // Arrange
            var testObject = new TestClass { Id = 1, Name = "Test" };

            // Act
            var json = JsonHelper.Serialize(testObject);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"id\":", json);
            Assert.Contains("\"name\":", json);
            Assert.Contains("\"Test\"", json);
        }

        [Fact]
        public void SerializeIndented_ValidObject_ReturnsIndentedJsonString()
        {
            // Arrange
            var testObject = new TestClass { Id = 1, Name = "Test" };

            // Act
            var json = JsonHelper.SerializeIndented(testObject);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("  \"id\":", json);
            Assert.Contains("  \"name\":", json);
            Assert.Contains("\"Test\"", json);
        }

        [Fact]
        public void Deserialize_ValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var obj = JsonHelper.Deserialize<TestClass>(json);

            // Assert
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
            Assert.Equal("Test", obj.Name);
        }

        [Fact]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonHelper.Deserialize<TestClass>(json));
        }

        [Fact]
        public void TryDeserialize_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = JsonHelper.TryDeserialize<TestClass>(json, out var obj);

            // Assert
            Assert.True(result);
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
            Assert.Equal("Test", obj.Name);
        }

        [Fact]
        public void TryDeserialize_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = JsonHelper.TryDeserialize<TestClass>(json, out var obj);

            // Assert
            Assert.False(result);
            Assert.Null(obj);
        }

        [Fact]
        public void IsValidJson_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = JsonHelper.IsValidJson(json);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = JsonHelper.IsValidJson(json);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidJson_Generic_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = JsonHelper.IsValidJson<TestClass>(json);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidJson_Generic_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = JsonHelper.IsValidJson<TestClass>(json);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateJson_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = JsonHelper.ValidateJson<TestClass>(json, out var errors);

            // Assert
            Assert.True(result);
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = JsonHelper.ValidateJson<TestClass>(json, out var errors);

            // Assert
            Assert.False(result);
            Assert.NotEmpty(errors);
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
