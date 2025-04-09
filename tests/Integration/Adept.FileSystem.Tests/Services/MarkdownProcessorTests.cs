using Adept.FileSystem.Tests.Fixtures;
using Adept.Services.FileSystem;
using System.Threading.Tasks;
using Xunit;

namespace Adept.FileSystem.Tests.Services
{
    /// <summary>
    /// Integration tests for the MarkdownProcessor
    /// </summary>
    public class MarkdownProcessorTests : IClassFixture<FileSystemTestFixture>
    {
        private readonly FileSystemTestFixture _fixture;

        public MarkdownProcessorTests(FileSystemTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExtractFrontMatter_MarkdownWithFrontMatter_ReturnsFrontMatter()
        {
            // Arrange
            var filePath = "test.md";
            var content = await _fixture.FileSystemService.ReadFileAsync(filePath);

            // Act
            var frontMatter = _fixture.MarkdownProcessor.ExtractFrontMatter(content);

            // Assert
            Assert.NotNull(frontMatter);
            Assert.Equal("Test Markdown File", frontMatter["title"]);
            Assert.Equal("This is a test markdown file", frontMatter["description"]);
            Assert.Equal("ADEPT", frontMatter["author"]);
            Assert.Equal("2023-06-01", frontMatter["date"]);
            Assert.Equal("test, markdown, adept", frontMatter["tags"]);
        }

        [Fact]
        public async Task ExtractMarkdownContent_MarkdownWithFrontMatter_ReturnsContentWithoutFrontMatter()
        {
            // Arrange
            var filePath = "test.md";
            var content = await _fixture.FileSystemService.ReadFileAsync(filePath);

            // Act
            var markdownContent = _fixture.MarkdownProcessor.ExtractMarkdownContent(content);

            // Assert
            Assert.NotNull(markdownContent);
            Assert.Contains("# Test Markdown File", markdownContent);
            Assert.Contains("This is a test markdown file created for integration tests.", markdownContent);
            Assert.Contains("## Features", markdownContent);
            Assert.DoesNotContain("---", markdownContent); // Front matter delimiters should be removed
            Assert.DoesNotContain("title:", markdownContent); // Front matter content should be removed
        }

        [Fact]
        public void ConvertMarkdownToHtml_ValidMarkdown_ReturnsHtml()
        {
            // Arrange
            var markdown = @"# Test Heading

This is a paragraph with **bold** and *italic* text.

## Subheading

- List item 1
- List item 2
- List item 3

[Link text](https://example.com)";

            // Act
            var html = _fixture.MarkdownProcessor.ConvertMarkdownToHtml(markdown);

            // Assert
            Assert.NotNull(html);
            Assert.Contains("<h1>Test Heading</h1>", html);
            Assert.Contains("<strong>bold</strong>", html);
            Assert.Contains("<em>italic</em>", html);
            Assert.Contains("<h2>Subheading</h2>", html);
            Assert.Contains("<li>List item", html);
            Assert.Contains("<a href=\"https://example.com\">Link text</a>", html);
        }

        [Fact]
        public void ExtractHeadings_ValidMarkdown_ReturnsHeadings()
        {
            // Arrange
            var markdown = @"# Main Heading

Some content here.

## Subheading 1

More content.

## Subheading 2

Even more content.

### Nested Heading

Nested content.

# Another Main Heading

Final content.";

            // Act
            var headings = _fixture.MarkdownProcessor.ExtractHeadings(markdown);

            // Assert
            Assert.Equal(5, headings.Count);
            Assert.Equal("Main Heading", headings[0].Text);
            Assert.Equal(1, headings[0].Level);
            Assert.Equal("Subheading 1", headings[1].Text);
            Assert.Equal(2, headings[1].Level);
            Assert.Equal("Subheading 2", headings[2].Text);
            Assert.Equal(2, headings[2].Level);
            Assert.Equal("Nested Heading", headings[3].Text);
            Assert.Equal(3, headings[3].Level);
            Assert.Equal("Another Main Heading", headings[4].Text);
            Assert.Equal(1, headings[4].Level);
        }
    }
}
