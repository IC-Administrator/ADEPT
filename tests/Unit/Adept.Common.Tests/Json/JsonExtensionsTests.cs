using Adept.Common.Json;
using System.Text.Json;
using Xunit;

namespace Adept.Common.Tests.Json
{
    public class JsonExtensionsTests
    {
        [Fact]
        public void ToJson_ValidObject_ReturnsJsonString()
        {
            // Arrange
            var testObject = new TestClass { Id = 1, Name = "Test" };

            // Act
            var json = testObject.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"id\":", json);
            Assert.Contains("\"name\":", json);
            Assert.Contains("\"Test\"", json);
        }

        [Fact]
        public void ToIndentedJson_ValidObject_ReturnsIndentedJsonString()
        {
            // Arrange
            var testObject = new TestClass { Id = 1, Name = "Test" };

            // Act
            var json = testObject.ToIndentedJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("  \"id\":", json);
            Assert.Contains("  \"name\":", json);
            Assert.Contains("\"Test\"", json);
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsObject()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var obj = json.FromJson<TestClass>();

            // Assert
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
            Assert.Equal("Test", obj.Name);
        }

        [Fact]
        public void FromJson_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act & Assert
            Assert.Throws<JsonException>(() => json.FromJson<TestClass>());
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = json.TryFromJson<TestClass>(out var obj);

            // Assert
            Assert.True(result);
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
            Assert.Equal("Test", obj.Name);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = json.TryFromJson<TestClass>(out var obj);

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
            var result = json.IsValidJson();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidJson_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = json.IsValidJson();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidJson_Generic_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = json.IsValidJson<TestClass>();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidJson_Generic_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\""; // Missing closing brace

            // Act
            var result = json.IsValidJson<TestClass>();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateJson_ValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"id\":1,\"name\":\"Test\"}";

            // Act
            var result = json.ValidateJson<TestClass>(out var errors);

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
            var result = json.ValidateJson<TestClass>(out var errors);

            // Assert
            Assert.False(result);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void DeepClone_ValidObject_ReturnsClone()
        {
            // Arrange
            var original = new TestClass { Id = 1, Name = "Test" };

            // Act
            var clone = original.DeepClone();

            // Assert
            Assert.NotNull(clone);
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
            Assert.NotSame(original, clone);
        }

        [Fact]
        public void ConvertTo_ValidObject_ReturnsConvertedObject()
        {
            // Arrange
            var source = new SourceClass { Id = 1, Name = "Test", Value = 42 };

            // Act
            var target = source.ConvertTo<SourceClass, TargetClass>();

            // Assert
            Assert.NotNull(target);
            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
            Assert.NotSame(source, target);
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class SourceClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        private class TargetClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
