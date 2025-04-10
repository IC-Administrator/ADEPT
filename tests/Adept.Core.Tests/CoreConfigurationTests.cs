using Xunit;
using Adept.Core.Configuration;

namespace Adept.Core.Tests
{
    public class CoreConfigurationTests
    {
        [Fact]
        public void AppSettings_ShouldHaveDefaultValues()
        {
            // Arrange
            var settings = new AppSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.False(string.IsNullOrEmpty(settings.ApplicationName));
            Assert.Equal("ADEPT", settings.ApplicationName);
        }

        [Fact]
        public void LlmSettings_ShouldHaveDefaultValues()
        {
            // Arrange
            var settings = new LlmSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.NotNull(settings.DefaultProvider);
            Assert.NotNull(settings.DefaultModel);
        }
    }
}
