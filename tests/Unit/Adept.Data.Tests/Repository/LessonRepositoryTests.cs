using Adept.Common.Interfaces;
using Adept.Core.Models;
using Adept.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Data.Tests.Repository
{
    /// <summary>
    /// Unit tests for the LessonRepository class
    /// </summary>
    public class LessonRepositoryTests
    {
        private readonly Mock<IDatabaseContext> _mockDatabaseContext;
        private readonly Mock<ILogger<LessonRepository>> _mockLogger;
        private readonly LessonRepository _repository;

        public LessonRepositoryTests()
        {
            _mockDatabaseContext = new Mock<IDatabaseContext>();
            _mockLogger = new Mock<ILogger<LessonRepository>>();
            _repository = new LessonRepository(_mockDatabaseContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllLessonPlansAsync_ReturnsLessonPlans()
        {
            // Arrange
            var expectedLessons = new List<LessonPlan>
            {
                new LessonPlan { LessonId = "1", ClassId = "class-1", Title = "Lesson 1", Date = "2023-01-01", TimeSlot = 0 },
                new LessonPlan { LessonId = "2", ClassId = "class-1", Title = "Lesson 2", Date = "2023-01-02", TimeSlot = 1 }
            };

            _mockDatabaseContext.Setup(x => x.QueryAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(expectedLessons);

            // Act
            var result = await _repository.GetAllLessonPlansAsync();

            // Assert
            Assert.Equal(expectedLessons, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<LessonPlan>(
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllLessonPlansAsync_WhenExceptionOccurs_ReturnsEmptyList()
        {
            // Arrange
            _mockDatabaseContext.Setup(x => x.QueryAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _repository.GetAllLessonPlansAsync();

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetLessonByIdAsync_WithValidId_ReturnsLessonPlan()
        {
            // Arrange
            var lessonId = "test-id";
            var expectedLesson = new LessonPlan { LessonId = lessonId, ClassId = "class-1", Title = "Test Lesson", Date = "2023-01-01", TimeSlot = 0 };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                                 p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId)))
                .ReturnsAsync(expectedLesson);

            // Act
            var result = await _repository.GetLessonByIdAsync(lessonId);

            // Assert
            Assert.Equal(expectedLesson, result);
            _mockDatabaseContext.Verify(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                             p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId)),
                Times.Once);
        }

        [Fact]
        public async Task GetLessonByIdAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonByIdAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonByIdAsync(""));
        }

        [Fact]
        public async Task GetLessonsByClassIdAsync_WithValidClassId_ReturnsLessonPlans()
        {
            // Arrange
            var classId = "class-1";
            var expectedLessons = new List<LessonPlan>
            {
                new LessonPlan { LessonId = "1", ClassId = classId, Title = "Lesson 1", Date = "2023-01-01", TimeSlot = 0 },
                new LessonPlan { LessonId = "2", ClassId = classId, Title = "Lesson 2", Date = "2023-01-02", TimeSlot = 1 }
            };

            _mockDatabaseContext.Setup(x => x.QueryAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)))
                .ReturnsAsync(expectedLessons);

            // Act
            var result = await _repository.GetLessonsByClassIdAsync(classId);

            // Assert
            Assert.Equal(expectedLessons, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<LessonPlan>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                             p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId)),
                Times.Once);
        }

        [Fact]
        public async Task GetLessonsByClassIdAsync_WithInvalidClassId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonsByClassIdAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonsByClassIdAsync(""));
        }

        [Fact]
        public async Task GetLessonsByDateAsync_WithValidDate_ReturnsLessonPlans()
        {
            // Arrange
            var date = "2023-01-01";
            var expectedLessons = new List<LessonPlan>
            {
                new LessonPlan { LessonId = "1", ClassId = "class-1", Title = "Lesson 1", Date = date, TimeSlot = 0 },
                new LessonPlan { LessonId = "2", ClassId = "class-2", Title = "Lesson 2", Date = date, TimeSlot = 1 }
            };

            _mockDatabaseContext.Setup(x => x.QueryAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("Date") != null &&
                                 p.GetType().GetProperty("Date").GetValue(p).ToString() == date)))
                .ReturnsAsync(expectedLessons);

            // Act
            var result = await _repository.GetLessonsByDateAsync(date);

            // Assert
            Assert.Equal(expectedLessons, result);
            _mockDatabaseContext.Verify(x => x.QueryAsync<LessonPlan>(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("Date") != null &&
                             p.GetType().GetProperty("Date").GetValue(p).ToString() == date)),
                Times.Once);
        }

        [Fact]
        public async Task GetLessonsByDateAsync_WithInvalidDate_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullDate = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonsByDateAsync(nullDate!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonsByDateAsync(""));
        }

        [Fact]
        public async Task GetLessonByClassDateSlotAsync_WithValidParameters_ReturnsLessonPlan()
        {
            // Arrange
            var classId = "class-1";
            var date = "2023-01-01";
            var timeSlot = 0;
            var expectedLesson = new LessonPlan { LessonId = "1", ClassId = classId, Title = "Test Lesson", Date = date, TimeSlot = timeSlot };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p =>
                        p != null &&
                        p.GetType().GetProperty("ClassId") != null &&
                        p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId &&
                        p.GetType().GetProperty("Date") != null &&
                        p.GetType().GetProperty("Date").GetValue(p).ToString() == date &&
                        p.GetType().GetProperty("TimeSlot") != null &&
                        Convert.ToInt32(p.GetType().GetProperty("TimeSlot").GetValue(p)) == timeSlot)))
                .ReturnsAsync(expectedLesson);

            // Act
            var result = await _repository.GetLessonByClassDateSlotAsync(classId, date, timeSlot);

            // Assert
            Assert.Equal(expectedLesson, result);
            _mockDatabaseContext.Verify(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                It.IsAny<string>(),
                It.Is<object>(p =>
                    p != null &&
                    p.GetType().GetProperty("ClassId") != null &&
                    p.GetType().GetProperty("ClassId").GetValue(p).ToString() == classId &&
                    p.GetType().GetProperty("Date") != null &&
                    p.GetType().GetProperty("Date").GetValue(p).ToString() == date &&
                    p.GetType().GetProperty("TimeSlot") != null &&
                    Convert.ToInt32(p.GetType().GetProperty("TimeSlot").GetValue(p)) == timeSlot)),
                Times.Once);
        }

        [Fact]
        public async Task GetLessonByClassDateSlotAsync_WithInvalidParameters_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonByClassDateSlotAsync(nullId!, "2023-01-01", 0));
            string? nullDate = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonByClassDateSlotAsync("class-1", nullDate!, 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonByClassDateSlotAsync("class-1", "2023-01-01", -1));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetLessonByClassDateSlotAsync("class-1", "2023-01-01", 5));
        }

        [Fact]
        public async Task AddLessonAsync_WithValidLesson_ReturnsLessonId()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Title = "Test Lesson",
                Date = "2023-01-01",
                TimeSlot = 0,
                LearningObjectives = "Test objectives"
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p =>
                        p != null &&
                        p.GetType().GetProperty("ClassId") != null &&
                        p.GetType().GetProperty("ClassId").GetValue(p).ToString() == lessonPlan.ClassId &&
                        p.GetType().GetProperty("Date") != null &&
                        p.GetType().GetProperty("Date").GetValue(p).ToString() == lessonPlan.Date &&
                        p.GetType().GetProperty("TimeSlot") != null &&
                        Convert.ToInt32(p.GetType().GetProperty("TimeSlot").GetValue(p)) == lessonPlan.TimeSlot)))
                .ReturnsAsync((LessonPlan?)null);

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<int>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == lessonPlan.ClassId)))
                .ReturnsAsync(1); // Class exists

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            // Act
            var result = await _repository.AddLessonAsync(lessonPlan);

            // Assert
            Assert.Equal(lessonPlan.LessonId, result);
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                             p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonPlan.LessonId)),
                Times.Once);
        }

        [Fact]
        public async Task AddLessonAsync_WithNullLesson_ThrowsArgumentNullException()
        {
            // Act & Assert
            LessonPlan? nullLesson = null;
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddLessonAsync(nullLesson!));
        }

        [Fact]
        public async Task AddLessonAsync_WithInvalidLesson_ThrowsArgumentException()
        {
            // Arrange
            var lessonWithoutTitle = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            var lessonWithoutDate = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Title = "Test Lesson",
                TimeSlot = 0
            };

            var lessonWithInvalidDate = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Title = "Test Lesson",
                Date = "invalid-date",
                TimeSlot = 0
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddLessonAsync(lessonWithoutTitle));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddLessonAsync(lessonWithoutDate));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddLessonAsync(lessonWithInvalidDate));
        }

        [Fact]
        public async Task AddLessonAsync_WithDuplicateLesson_ThrowsInvalidOperationException()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Title = "Test Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            var existingLesson = new LessonPlan
            {
                LessonId = "existing-id",
                ClassId = "class-1",
                Title = "Existing Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p =>
                        p != null &&
                        p.GetType().GetProperty("ClassId") != null &&
                        p.GetType().GetProperty("ClassId").GetValue(p).ToString() == lessonPlan.ClassId &&
                        p.GetType().GetProperty("Date") != null &&
                        p.GetType().GetProperty("Date").GetValue(p).ToString() == lessonPlan.Date &&
                        p.GetType().GetProperty("TimeSlot") != null &&
                        Convert.ToInt32(p.GetType().GetProperty("TimeSlot").GetValue(p)) == lessonPlan.TimeSlot)))
                .ReturnsAsync(existingLesson);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddLessonAsync(lessonPlan));
        }

        [Fact]
        public async Task AddLessonAsync_WithNonExistentClass_ThrowsInvalidOperationException()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "non-existent-class",
                Title = "Test Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p =>
                        p != null &&
                        p.GetType().GetProperty("ClassId") != null &&
                        p.GetType().GetProperty("ClassId").GetValue(p).ToString() == lessonPlan.ClassId &&
                        p.GetType().GetProperty("Date") != null &&
                        p.GetType().GetProperty("Date").GetValue(p).ToString() == lessonPlan.Date &&
                        p.GetType().GetProperty("TimeSlot") != null &&
                        Convert.ToInt32(p.GetType().GetProperty("TimeSlot").GetValue(p)) == lessonPlan.TimeSlot)))
                .ReturnsAsync((LessonPlan?)null);

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<int>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == lessonPlan.ClassId)))
                .ReturnsAsync(0); // Class does not exist

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddLessonAsync(lessonPlan));
        }

        [Fact]
        public async Task UpdateLessonAsync_WithValidLesson_UpdatesLesson()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Title = "Updated Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            var existingLesson = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Title = "Original Lesson",
                Date = "2023-01-01",
                TimeSlot = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                                 p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonPlan.LessonId)))
                .ReturnsAsync(existingLesson);

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<int>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("ClassId") != null &&
                                 p.GetType().GetProperty("ClassId").GetValue(p).ToString() == lessonPlan.ClassId)))
                .ReturnsAsync(1); // Class exists

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(1);

            // Act
            await _repository.UpdateLessonAsync(lessonPlan);

            // Assert
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                             p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonPlan.LessonId)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateLessonAsync_WithNullLesson_ThrowsArgumentException()
        {
            // Act & Assert
            LessonPlan? nullLesson = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateLessonAsync(nullLesson!));
        }

        [Fact]
        public async Task UpdateLessonAsync_WithoutId_ThrowsInvalidOperationException()
        {
            // Arrange
            var lessonWithoutId = new LessonPlan
            {
                ClassId = "class-1",
                Title = "Test Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            // Make sure the mock doesn't return a lesson when queried
            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync((LessonPlan?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateLessonAsync(lessonWithoutId));
        }

        [Fact]
        public async Task UpdateLessonAsync_WithoutTitle_ThrowsArgumentException()
        {
            // Arrange
            var lessonWithoutTitle = new LessonPlan
            {
                LessonId = "test-id",
                ClassId = "class-1",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateLessonAsync(lessonWithoutTitle));
        }

        [Fact]
        public async Task UpdateLessonAsync_WithNonExistentLesson_ThrowsInvalidOperationException()
        {
            // Arrange
            var lessonPlan = new LessonPlan
            {
                LessonId = "non-existent-id",
                ClassId = "class-1",
                Title = "Test Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                                 p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonPlan.LessonId)))
                .ReturnsAsync((LessonPlan?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateLessonAsync(lessonPlan));
        }

        [Fact]
        public async Task DeleteLessonAsync_WithValidId_DeletesLesson()
        {
            // Arrange
            var lessonId = "test-id";
            var existingLesson = new LessonPlan
            {
                LessonId = lessonId,
                ClassId = "class-1",
                Title = "Test Lesson",
                Date = "2023-01-01",
                TimeSlot = 0
            };

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                                 p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId)))
                .ReturnsAsync(existingLesson);

            var mockTransaction = new Mock<IDbTransaction>();
            _mockDatabaseContext.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(mockTransaction.Object);

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                                 p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId)))
                .ReturnsAsync(1);

            // Act
            bool result = await _repository.DeleteLessonAsync(lessonId);

            // Assert
            Assert.True(result);
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                             p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId)),
                Times.Once);
        }

        [Fact]
        public async Task DeleteLessonAsync_WithInvalidId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteLessonAsync(nullId!));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteLessonAsync(""));
        }

        [Fact]
        public async Task DeleteLessonAsync_WithNonExistentLesson_ReturnsFalse()
        {
            // Arrange
            var lessonId = "non-existent-id";

            _mockDatabaseContext.Setup(x => x.QuerySingleOrDefaultAsync<LessonPlan>(
                    It.IsAny<string>(),
                    It.Is<object>(p => p != null && p.GetType().GetProperty("LessonId") != null &&
                                 p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId)))
                .ReturnsAsync((LessonPlan?)null);

            // Act
            bool result = await _repository.DeleteLessonAsync(lessonId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCalendarEventIdAsync_WithValidParameters_UpdatesCalendarEventId()
        {
            // Arrange
            var lessonId = "test-id";
            var calendarEventId = "calendar-event-id";

            _mockDatabaseContext.Setup(x => x.ExecuteNonQueryAsync(
                    It.IsAny<string>(),
                    It.Is<object>(p =>
                        p != null &&
                        p.GetType().GetProperty("LessonId") != null &&
                        p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId &&
                        p.GetType().GetProperty("CalendarEventId") != null &&
                        p.GetType().GetProperty("CalendarEventId").GetValue(p).ToString() == calendarEventId)))
                .ReturnsAsync(1);

            // Act
            await _repository.UpdateCalendarEventIdAsync(lessonId, calendarEventId);

            // Assert
            _mockDatabaseContext.Verify(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.Is<object>(p =>
                    p != null &&
                    p.GetType().GetProperty("LessonId") != null &&
                    p.GetType().GetProperty("LessonId").GetValue(p).ToString() == lessonId &&
                    p.GetType().GetProperty("CalendarEventId") != null &&
                    p.GetType().GetProperty("CalendarEventId").GetValue(p).ToString() == calendarEventId)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateCalendarEventIdAsync_WithInvalidLessonId_ThrowsArgumentException()
        {
            // Act & Assert
            string? nullId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateCalendarEventIdAsync(nullId!, "calendar-event-id"));
            await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateCalendarEventIdAsync("", "calendar-event-id"));
        }
    }
}
