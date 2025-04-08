using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adept.Services.Calendar
{
    /// <summary>
    /// Mock implementation of the lesson plan service for testing
    /// </summary>
    public class MockLessonPlanService : ILessonPlanService
    {
        private readonly ILogger<MockLessonPlanService> _logger;
        private readonly List<LessonPlanInfo> _lessonPlans = new List<LessonPlanInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MockLessonPlanService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public MockLessonPlanService(ILogger<MockLessonPlanService> logger)
        {
            _logger = logger;

            // Add some mock lesson plans
            _lessonPlans.Add(new LessonPlanInfo
            {
                Id = 1,
                Title = "Introduction to Programming",
                Description = "Basic programming concepts",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                ClassId = 1,
                Date = DateTime.Now,
                LearningObjectives = "Understand basic programming concepts",
                LessonComponents = "Lecture, Exercises"
            });

            _lessonPlans.Add(new LessonPlanInfo
            {
                Id = 2,
                Title = "Advanced Programming",
                Description = "Advanced programming concepts",
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(1).AddHours(1),
                ClassId = 1,
                Date = DateTime.Now.AddDays(1),
                LearningObjectives = "Understand advanced programming concepts",
                LessonComponents = "Lecture, Exercises, Project"
            });
        }

        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="id">The lesson plan ID</param>
        /// <returns>The lesson plan</returns>
        public Task<LessonPlanInfo> GetLessonPlanAsync(int id)
        {
            _logger.LogInformation("Mock: Getting lesson plan {Id}", id);
            var lessonPlan = _lessonPlans.Find(lp => lp.Id == id);
            return Task.FromResult(lessonPlan ?? new LessonPlanInfo());
        }

        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>List of lesson plans</returns>
        public Task<IEnumerable<LessonPlanInfo>> GetLessonPlansAsync()
        {
            _logger.LogInformation("Mock: Getting all lesson plans");
            return Task.FromResult<IEnumerable<LessonPlanInfo>>(_lessonPlans);
        }

        /// <summary>
        /// Gets a lesson plan by ID
        /// </summary>
        /// <param name="id">The lesson plan ID</param>
        /// <returns>The lesson plan</returns>
        public Task<LessonPlanInfo> GetLessonPlanByIdAsync(int id)
        {
            _logger.LogInformation("Mock: Getting lesson plan by ID {Id}", id);
            var lessonPlan = _lessonPlans.Find(lp => lp.Id == id);
            return Task.FromResult(lessonPlan ?? new LessonPlanInfo());
        }

        /// <summary>
        /// Gets all lesson plans
        /// </summary>
        /// <returns>List of lesson plans</returns>
        public Task<IEnumerable<LessonPlanInfo>> GetAllLessonPlansAsync()
        {
            _logger.LogInformation("Mock: Getting all lesson plans");
            return Task.FromResult<IEnumerable<LessonPlanInfo>>(_lessonPlans);
        }

        /// <summary>
        /// Updates a lesson plan
        /// </summary>
        /// <param name="lessonPlan">The lesson plan to update</param>
        /// <returns>True if successful</returns>
        public Task<bool> UpdateLessonPlanAsync(LessonPlanInfo lessonPlan)
        {
            _logger.LogInformation("Mock: Updating lesson plan {Id}", lessonPlan.Id);
            var index = _lessonPlans.FindIndex(lp => lp.Id == lessonPlan.Id);
            if (index >= 0)
            {
                _lessonPlans[index] = lessonPlan;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
