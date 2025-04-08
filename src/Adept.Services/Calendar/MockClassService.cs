using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Mock implementation of the class service for testing
    /// </summary>
    public class MockClassService : IClassService
    {
        private readonly ILogger<MockClassService> _logger;
        private readonly List<ClassInfo> _classes = new List<ClassInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MockClassService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockClassService(ILogger<MockClassService> logger)
        {
            _logger = logger;

            // Add some mock classes
            _classes.Add(new ClassInfo
            {
                Id = 1,
                Name = "Programming 101",
                Description = "Introduction to programming",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                StartTime = new TimeSpan(9, 0, 0),
                DurationMinutes = 60,
                Subject = "Computer Science",
                Location = "Room 101"
            });

            _classes.Add(new ClassInfo
            {
                Id = 2,
                Name = "Programming 201",
                Description = "Advanced programming",
                StartDate = DateTime.Now.AddMonths(3),
                EndDate = DateTime.Now.AddMonths(6),
                StartTime = new TimeSpan(10, 0, 0),
                DurationMinutes = 90,
                Subject = "Computer Science",
                Location = "Room 201"
            });
        }

        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="id">The class ID</param>
        /// <returns>The class</returns>
        public Task<ClassInfo> GetClassAsync(int id)
        {
            _logger.LogInformation("Mock: Getting class {Id}", id);
            var classInfo = _classes.Find(c => c.Id == id);
            return Task.FromResult(classInfo ?? new ClassInfo());
        }

        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>List of classes</returns>
        public Task<IEnumerable<ClassInfo>> GetClassesAsync()
        {
            _logger.LogInformation("Mock: Getting all classes");
            return Task.FromResult<IEnumerable<ClassInfo>>(_classes);
        }

        /// <summary>
        /// Gets a class by ID
        /// </summary>
        /// <param name="id">The class ID</param>
        /// <returns>The class</returns>
        public Task<ClassInfo> GetClassByIdAsync(int id)
        {
            _logger.LogInformation("Mock: Getting class by ID {Id}", id);
            var classInfo = _classes.Find(c => c.Id == id);
            return Task.FromResult(classInfo ?? new ClassInfo());
        }

        /// <summary>
        /// Gets all classes
        /// </summary>
        /// <returns>List of classes</returns>
        public Task<IEnumerable<ClassInfo>> GetAllClassesAsync()
        {
            _logger.LogInformation("Mock: Getting all classes");
            return Task.FromResult<IEnumerable<ClassInfo>>(_classes);
        }
    }
}
