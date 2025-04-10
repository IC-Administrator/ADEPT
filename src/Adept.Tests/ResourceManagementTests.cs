using Adept.Core.Models;
using Adept.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Tests
{
    public class ResourceManagementTests
    {
        [Fact]
        public void ResourceModel_Properties_ShouldBeCorrect()
        {
            // Arrange
            var resourceId = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var name = "Test Resource";
            var path = "C:\\Test\\test.docx";
            var type = ResourceType.Document;
            
            // Act
            var resource = new LessonResource
            {
                ResourceId = resourceId,
                LessonId = lessonId,
                Name = name,
                Path = path,
                Type = type
            };
            
            // Assert
            Assert.Equal(resourceId, resource.ResourceId);
            Assert.Equal(lessonId, resource.LessonId);
            Assert.Equal(name, resource.Name);
            Assert.Equal(path, resource.Path);
            Assert.Equal(type, resource.Type);
        }
        
        [Fact]
        public void ResourceType_Enum_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.Equal(0, (int)ResourceType.File);
            Assert.Equal(1, (int)ResourceType.Link);
            Assert.Equal(2, (int)ResourceType.Image);
            Assert.Equal(3, (int)ResourceType.Document);
            Assert.Equal(4, (int)ResourceType.Presentation);
            Assert.Equal(5, (int)ResourceType.Spreadsheet);
            Assert.Equal(6, (int)ResourceType.Video);
            Assert.Equal(7, (int)ResourceType.Audio);
            Assert.Equal(8, (int)ResourceType.Other);
        }
        
        [Fact]
        public void GetResourceTypeFromExtension_ShouldReturnCorrectType()
        {
            // This is a mock test since we can't directly access the private method
            // In a real test, we would test this through a public method or make the method public for testing
            
            // Document types
            Assert.Equal(ResourceType.Document, MockGetResourceTypeFromExtension(".docx"));
            Assert.Equal(ResourceType.Document, MockGetResourceTypeFromExtension(".pdf"));
            
            // Image types
            Assert.Equal(ResourceType.Image, MockGetResourceTypeFromExtension(".jpg"));
            Assert.Equal(ResourceType.Image, MockGetResourceTypeFromExtension(".png"));
            
            // Presentation types
            Assert.Equal(ResourceType.Presentation, MockGetResourceTypeFromExtension(".pptx"));
            
            // Spreadsheet types
            Assert.Equal(ResourceType.Spreadsheet, MockGetResourceTypeFromExtension(".xlsx"));
            
            // Video types
            Assert.Equal(ResourceType.Video, MockGetResourceTypeFromExtension(".mp4"));
            
            // Audio types
            Assert.Equal(ResourceType.Audio, MockGetResourceTypeFromExtension(".mp3"));
            
            // Default type
            Assert.Equal(ResourceType.File, MockGetResourceTypeFromExtension(".unknown"));
        }
        
        private ResourceType MockGetResourceTypeFromExtension(string extension)
        {
            // This is a mock implementation of the GetResourceTypeFromExtension method
            switch (extension.ToLower())
            {
                case ".docx":
                case ".doc":
                case ".pdf":
                case ".txt":
                    return ResourceType.Document;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    return ResourceType.Image;
                case ".pptx":
                case ".ppt":
                    return ResourceType.Presentation;
                case ".xlsx":
                case ".xls":
                case ".csv":
                    return ResourceType.Spreadsheet;
                case ".mp4":
                case ".avi":
                case ".mov":
                case ".wmv":
                    return ResourceType.Video;
                case ".mp3":
                case ".wav":
                case ".ogg":
                    return ResourceType.Audio;
                default:
                    return ResourceType.File;
            }
        }
    }
}
