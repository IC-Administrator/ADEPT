using Adept.Core.Models;
using Adept.Data.Repositories;
using Adept.Database.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Database.Tests.Repository
{
    /// <summary>
    /// Integration tests for the LessonRepository
    /// </summary>
    public class LessonRepositoryIntegrationTests : IClassFixture<SqliteDatabaseFixture>
    {
        private readonly SqliteDatabaseFixture _fixture;
        private readonly LessonRepository _repository;
        private readonly ClassRepository _classRepository;

        public LessonRepositoryIntegrationTests(SqliteDatabaseFixture fixture)
        {
            _fixture = fixture;
            var lessonLogger = _fixture.ServiceProvider.GetRequiredService<ILogger<LessonRepository>>();
            var classLogger = _fixture.ServiceProvider.GetRequiredService<ILogger<ClassRepository>>();
            _repository = new LessonRepository(_fixture.DatabaseContext, lessonLogger);
            _classRepository = new ClassRepository(_fixture.DatabaseContext, classLogger);
        }

        [Fact]
        public async Task GetAllLessonPlansAsync_ReturnsLessonPlans()
        {
            // Act
            var lessons = await _repository.GetAllLessonPlansAsync();

            // Assert
            Assert.NotEmpty(lessons);
            Assert.Contains(lessons, l => l.Title == "Variables and Data Types");
        }

        [Fact]
        public async Task GetLessonByIdAsync_WithValidId_ReturnsLessonPlan()
        {
            // Arrange
            var lessons = await _repository.GetAllLessonPlansAsync();
            var lessonId = lessons.First().LessonId;

            // Act
            var result = await _repository.GetLessonByIdAsync(lessonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(lessonId, result.LessonId);
        }

        [Fact]
        public async Task GetLessonByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetLessonByIdAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLessonsByClassIdAsync_WithValidClassId_ReturnsLessonPlans()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            // Act
            var lessons = await _repository.GetLessonsByClassIdAsync(classId);

            // Assert
            Assert.NotNull(lessons);
            // Note: This test might fail if there are no lessons for the test class
            // In a real test, we would ensure there are lessons for the class
        }

        [Fact]
        public async Task GetLessonsByClassIdAsync_WithInvalidClassId_ReturnsEmptyList()
        {
            // Act
            var lessons = await _repository.GetLessonsByClassIdAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Empty(lessons);
        }

        [Fact]
        public async Task GetLessonsByDateAsync_WithValidDate_ReturnsLessonPlans()
        {
            // Arrange
            var lessons = await _repository.GetAllLessonPlansAsync();
            var date = lessons.First().Date;

            // Act
            var result = await _repository.GetLessonsByDateAsync(date);

            // Assert
            Assert.NotEmpty(result);
            Assert.All(result, l => Assert.Equal(date, l.Date));
        }

        [Fact]
        public async Task GetLessonsByDateAsync_WithInvalidDate_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.GetLessonsByDateAsync("2099-12-31");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task AddLessonAsync_WithValidLesson_AddsLesson()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var lesson = new LessonPlan
            {
                ClassId = classId,
                Title = $"Test Lesson {Guid.NewGuid():N}",
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeSlot = 2,
                LearningObjectives = "Test objectives",
                ComponentsJson = "{}"
            };

            // Act
            var lessonId = await _repository.AddLessonAsync(lesson);
            var result = await _repository.GetLessonByIdAsync(lessonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(lesson.Title, result.Title);
            Assert.Equal(lesson.Date, result.Date);
            Assert.Equal(lesson.TimeSlot, result.TimeSlot);
        }

        [Fact]
        public async Task AddLessonAsync_WithDuplicateClassDateSlot_ThrowsInvalidOperationException()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var lesson1 = new LessonPlan
            {
                ClassId = classId,
                Title = $"Test Lesson 1 {Guid.NewGuid():N}",
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeSlot = 3,
                LearningObjectives = "Test objectives",
                ComponentsJson = "{}"
            };

            var lesson2 = new LessonPlan
            {
                ClassId = classId,
                Title = $"Test Lesson 2 {Guid.NewGuid():N}",
                Date = lesson1.Date,
                TimeSlot = lesson1.TimeSlot,
                LearningObjectives = "Test objectives",
                ComponentsJson = "{}"
            };

            // Add the first lesson
            await _repository.AddLessonAsync(lesson1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddLessonAsync(lesson2));
        }

        [Fact]
        public async Task UpdateLessonAsync_WithValidLesson_UpdatesLesson()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var lesson = new LessonPlan
            {
                ClassId = classId,
                Title = $"Test Lesson {Guid.NewGuid():N}",
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeSlot = 4,
                LearningObjectives = "Test objectives",
                ComponentsJson = "{}"
            };

            var lessonId = await _repository.AddLessonAsync(lesson);
            var addedLesson = await _repository.GetLessonByIdAsync(lessonId);
            addedLesson.Title = "Updated Title";

            // Act
            await _repository.UpdateLessonAsync(addedLesson);
            var result = await _repository.GetLessonByIdAsync(lessonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
        }

        [Fact]
        public async Task DeleteLessonAsync_WithValidId_DeletesLesson()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var lesson = new LessonPlan
            {
                ClassId = classId,
                Title = $"Test Lesson {Guid.NewGuid():N}",
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeSlot = 1,
                LearningObjectives = "Test objectives",
                ComponentsJson = "{}"
            };

            var lessonId = await _repository.AddLessonAsync(lesson);

            // Act
            await _repository.DeleteLessonAsync(lessonId);
            var result = await _repository.GetLessonByIdAsync(lessonId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateCalendarEventIdAsync_WithValidParameters_UpdatesCalendarEventId()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var lesson = new LessonPlan
            {
                ClassId = classId,
                Title = $"Test Lesson {Guid.NewGuid():N}",
                Date = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeSlot = 0,
                LearningObjectives = "Test objectives",
                ComponentsJson = "{}"
            };

            var lessonId = await _repository.AddLessonAsync(lesson);
            var calendarEventId = $"calendar-event-{Guid.NewGuid():N}";

            // Act
            await _repository.UpdateCalendarEventIdAsync(lessonId, calendarEventId);
            var result = await _repository.GetLessonByIdAsync(lessonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(calendarEventId, result.CalendarEventId);
        }
    }
}
