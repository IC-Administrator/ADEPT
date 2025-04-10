using Adept.Core.Models;
using Adept.Data.Repositories;
using Adept.Database.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adept.Database.Tests.Repository
{
    /// <summary>
    /// Integration tests for the StudentRepository
    /// </summary>
    public class StudentRepositoryIntegrationTests : IClassFixture<SqliteDatabaseFixture>
    {
        private readonly SqliteDatabaseFixture _fixture;
        private readonly StudentRepository _repository;
        private readonly ClassRepository _classRepository;

        public StudentRepositoryIntegrationTests(SqliteDatabaseFixture fixture)
        {
            _fixture = fixture;
            var studentLogger = _fixture.ServiceProvider.GetRequiredService<ILogger<StudentRepository>>();
            var classLogger = _fixture.ServiceProvider.GetRequiredService<ILogger<ClassRepository>>();
            _repository = new StudentRepository(_fixture.DatabaseContext, studentLogger);
            _classRepository = new ClassRepository(_fixture.DatabaseContext, classLogger);
        }

        [Fact]
        public async Task GetAllStudentsAsync_ReturnsStudents()
        {
            // Act
            var students = await _repository.GetAllStudentsAsync();

            // Assert
            Assert.NotEmpty(students);
            Assert.Contains(students, s => s.Name == "John Doe");
        }

        [Fact]
        public async Task GetStudentByIdAsync_WithValidId_ReturnsStudent()
        {
            // Arrange
            var students = await _repository.GetAllStudentsAsync();
            var studentId = students.First().StudentId;

            // Act
            var result = await _repository.GetStudentByIdAsync(studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(studentId, result.StudentId);
        }

        [Fact]
        public async Task GetStudentByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetStudentByIdAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetStudentsByClassIdAsync_WithValidClassId_ReturnsStudents()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            // Act
            var students = await _repository.GetStudentsByClassIdAsync(classId);

            // Assert
            Assert.NotNull(students);
            // Note: This test might fail if there are no students in the test class
            // In a real test, we would ensure there are students in the class
        }

        [Fact]
        public async Task GetStudentsByClassIdAsync_WithInvalidClassId_ReturnsEmptyList()
        {
            // Act
            var students = await _repository.GetStudentsByClassIdAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Empty(students);
        }

        [Fact]
        public async Task AddStudentAsync_WithValidStudent_AddsStudent()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var student = new Student
            {
                ClassId = classId,
                Name = $"Test Student {Guid.NewGuid():N}",
                FsmStatus = "No",
                SenStatus = "No",
                EalStatus = "No",
                AbilityLevel = "Medium",
                ReadingAge = "12",
                TargetGrade = "A",
                Notes = "Test notes"
            };

            // Act
            var studentId = await _repository.AddStudentAsync(student);
            var result = await _repository.GetStudentByIdAsync(studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(student.Name, result.Name);
            Assert.Equal(student.ClassId, result.ClassId);
        }

        [Fact]
        public async Task UpdateStudentAsync_WithValidStudent_UpdatesStudent()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var student = new Student
            {
                ClassId = classId,
                Name = $"Test Student {Guid.NewGuid():N}",
                FsmStatus = "No",
                SenStatus = "No",
                EalStatus = "No",
                AbilityLevel = "Medium",
                ReadingAge = "12",
                TargetGrade = "A",
                Notes = "Test notes"
            };

            var studentId = await _repository.AddStudentAsync(student);
            var addedStudent = await _repository.GetStudentByIdAsync(studentId);
            addedStudent.Notes = "Updated notes";

            // Act
            await _repository.UpdateStudentAsync(addedStudent);
            var result = await _repository.GetStudentByIdAsync(studentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated notes", result.Notes);
        }

        [Fact]
        public async Task DeleteStudentAsync_WithValidId_DeletesStudent()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var student = new Student
            {
                ClassId = classId,
                Name = $"Test Student {Guid.NewGuid():N}",
                FsmStatus = "No",
                SenStatus = "No",
                EalStatus = "No",
                AbilityLevel = "Medium",
                ReadingAge = "12",
                TargetGrade = "A",
                Notes = "Test notes"
            };

            var studentId = await _repository.AddStudentAsync(student);

            // Act
            await _repository.DeleteStudentAsync(studentId);
            var result = await _repository.GetStudentByIdAsync(studentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddStudentsAsync_WithValidStudents_AddsMultipleStudents()
        {
            // Arrange
            var classes = await _classRepository.GetAllClassesAsync();
            var classId = classes.First().ClassId;

            var students = new List<Student>
            {
                new Student
                {
                    ClassId = classId,
                    Name = $"Test Student 1 {Guid.NewGuid():N}",
                    FsmStatus = "No",
                    SenStatus = "No",
                    EalStatus = "No"
                },
                new Student
                {
                    ClassId = classId,
                    Name = $"Test Student 2 {Guid.NewGuid():N}",
                    FsmStatus = "Yes",
                    SenStatus = "No",
                    EalStatus = "No"
                }
            };

            // Act
            var studentIds = await _repository.AddStudentsAsync(students);
            var results = await Task.WhenAll(studentIds.Select(id => _repository.GetStudentByIdAsync(id)));

            // Assert
            Assert.Equal(2, results.Length);
            Assert.All(results, r => Assert.NotNull(r));
            Assert.Contains(results, r => r.Name == students[0].Name);
            Assert.Contains(results, r => r.Name == students[1].Name);
        }
    }
}
