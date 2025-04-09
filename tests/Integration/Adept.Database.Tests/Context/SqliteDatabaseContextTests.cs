using Adept.Database.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Database.Tests.Context
{
    /// <summary>
    /// Integration tests for the SqliteDatabaseContext
    /// </summary>
    public class SqliteDatabaseContextTests : IClassFixture<SqliteDatabaseFixture>
    {
        private readonly SqliteDatabaseFixture _fixture;

        public SqliteDatabaseContextTests(SqliteDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ExecuteQueryAsync_ReturnsResults()
        {
            // Arrange
            var query = "SELECT * FROM Classes WHERE ClassCode = @ClassCode";
            var parameters = new { ClassCode = "CS101" };

            // Act
            var results = await _fixture.DatabaseContext.ExecuteQueryAsync<ClassDto>(query, parameters);

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal("CS101", results[0].ClassCode);
            Assert.Equal("Introduction to Computer Science", results[0].ClassName);
        }

        [Fact]
        public async Task ExecuteScalarAsync_ReturnsValue()
        {
            // Arrange
            var query = "SELECT COUNT(*) FROM Classes";

            // Act
            var count = await _fixture.DatabaseContext.ExecuteScalarAsync<long>(query, null);

            // Assert
            Assert.True(count > 0);
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_InsertsData()
        {
            // Arrange
            var studentId = Guid.NewGuid().ToString();
            var query = @"
                INSERT INTO Students (StudentId, Name, Email, EnrollmentDate)
                VALUES (@StudentId, @Name, @Email, @EnrollmentDate)";
            var parameters = new
            {
                StudentId = studentId,
                Name = "Jane Smith",
                Email = "jane.smith@example.com",
                EnrollmentDate = DateTime.Now.ToString("o")
            };

            // Act
            await _fixture.DatabaseContext.ExecuteNonQueryAsync(query, parameters);

            // Assert
            var verifyQuery = "SELECT COUNT(*) FROM Students WHERE StudentId = @StudentId";
            var count = await _fixture.DatabaseContext.ExecuteScalarAsync<long>(verifyQuery, new { StudentId = studentId });
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task ExecuteTransactionAsync_CommitsChanges()
        {
            // Arrange
            var classId = Guid.NewGuid().ToString();
            var lessonId = Guid.NewGuid().ToString();

            // Act
            await _fixture.DatabaseContext.ExecuteTransactionAsync(async (transaction) =>
            {
                // Insert a class
                await transaction.ExecuteNonQueryAsync(@"
                    INSERT INTO Classes (ClassId, ClassCode, ClassName, EducationLevel, CurrentTopic, CreatedDate)
                    VALUES (@ClassId, @ClassCode, @ClassName, @EducationLevel, @CurrentTopic, @CreatedDate);
                ", new
                {
                    ClassId = classId,
                    ClassCode = "CS102",
                    ClassName = "Data Structures",
                    EducationLevel = "Undergraduate",
                    CurrentTopic = "Arrays and Lists",
                    CreatedDate = DateTime.Now.ToString("o")
                });

                // Insert a lesson for the class
                await transaction.ExecuteNonQueryAsync(@"
                    INSERT INTO Lessons (LessonId, ClassId, Title, Content, CreatedDate)
                    VALUES (@LessonId, @ClassId, @Title, @Content, @CreatedDate);
                ", new
                {
                    LessonId = lessonId,
                    ClassId = classId,
                    Title = "Arrays and Lists",
                    Content = "This lesson covers arrays and lists in programming.",
                    CreatedDate = DateTime.Now.ToString("o")
                });

                return true; // Commit the transaction
            });

            // Assert
            var classCount = await _fixture.DatabaseContext.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM Classes WHERE ClassId = @ClassId", new { ClassId = classId });
            var lessonCount = await _fixture.DatabaseContext.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM Lessons WHERE LessonId = @LessonId", new { LessonId = lessonId });

            Assert.Equal(1, classCount);
            Assert.Equal(1, lessonCount);
        }

        [Fact]
        public async Task ExecuteTransactionAsync_RollsBackOnFailure()
        {
            // Arrange
            var classId = Guid.NewGuid().ToString();
            var invalidLessonId = Guid.NewGuid().ToString();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _fixture.DatabaseContext.ExecuteTransactionAsync(async (transaction) =>
                {
                    // Insert a class
                    await transaction.ExecuteNonQueryAsync(@"
                        INSERT INTO Classes (ClassId, ClassCode, ClassName, EducationLevel, CurrentTopic, CreatedDate)
                        VALUES (@ClassId, @ClassCode, @ClassName, @EducationLevel, @CurrentTopic, @CreatedDate);
                    ", new
                    {
                        ClassId = classId,
                        ClassCode = "CS103",
                        ClassName = "Algorithms",
                        EducationLevel = "Undergraduate",
                        CurrentTopic = "Sorting Algorithms",
                        CreatedDate = DateTime.Now.ToString("o")
                    });

                    // Try to insert a lesson with an invalid foreign key (non-existent ClassId)
                    await transaction.ExecuteNonQueryAsync(@"
                        INSERT INTO Lessons (LessonId, ClassId, Title, Content, CreatedDate)
                        VALUES (@LessonId, @ClassId, @Title, @Content, @CreatedDate);
                    ", new
                    {
                        LessonId = invalidLessonId,
                        ClassId = "non-existent-class-id", // This will violate the foreign key constraint
                        Title = "Sorting Algorithms",
                        Content = "This lesson covers sorting algorithms.",
                        CreatedDate = DateTime.Now.ToString("o")
                    });

                    return true; // This won't be reached due to the exception
                });
            });

            // Verify that nothing was committed
            var classCount = await _fixture.DatabaseContext.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM Classes WHERE ClassId = @ClassId", new { ClassId = classId });
            var lessonCount = await _fixture.DatabaseContext.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM Lessons WHERE LessonId = @LessonId", new { LessonId = invalidLessonId });

            Assert.Equal(0, classCount);
            Assert.Equal(0, lessonCount);
        }

        /// <summary>
        /// DTO for Class entity
        /// </summary>
        private class ClassDto
        {
            public string ClassId { get; set; } = string.Empty;
            public string ClassCode { get; set; } = string.Empty;
            public string? ClassName { get; set; }
            public string? EducationLevel { get; set; }
            public string? CurrentTopic { get; set; }
            public string CreatedDate { get; set; } = string.Empty;
        }
    }
}
