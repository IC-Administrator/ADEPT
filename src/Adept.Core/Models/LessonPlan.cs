using Adept.Common.Json;

namespace Adept.Core.Models
{
    /// <summary>
    /// Represents a lesson plan
    /// </summary>
    public class LessonPlan
    {
        /// <summary>
        /// Unique identifier for the lesson
        /// </summary>
        public string LessonId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Class ID this lesson is for
        /// </summary>
        public string ClassId { get; set; } = string.Empty;

        /// <summary>
        /// Date of the lesson (YYYY-MM-DD)
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// Time slot (0-4, representing the 5 periods in a day)
        /// </summary>
        public int TimeSlot { get; set; }

        /// <summary>
        /// Lesson title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Learning objectives
        /// </summary>
        public string? LearningObjectives { get; set; }

        /// <summary>
        /// Google Calendar event ID if synchronized
        /// </summary>
        public string? CalendarEventId { get; set; }

        /// <summary>
        /// Lesson components stored as JSON
        /// </summary>
        public string ComponentsJson { get; set; } = "{}";

        /// <summary>
        /// When the lesson plan was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the lesson plan was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the lesson components
        /// </summary>
        public LessonComponents Components
        {
            get
            {
                if (string.IsNullOrEmpty(ComponentsJson) || ComponentsJson == "{}")
                {
                    return new LessonComponents();
                }

                if (ComponentsJson.TryFromJson<LessonComponents>(out var components) && components != null)
                {
                    return components;
                }

                return new LessonComponents();
            }
            set
            {
                ComponentsJson = value.ToJson();
            }
        }
    }

    /// <summary>
    /// Represents the components of a lesson plan
    /// </summary>
    public class LessonPlanComponents
    {
        /// <summary>
        /// Retrieval questions with answers
        /// </summary>
        public List<QuestionAnswer> RetrievalQuestions { get; set; } = new List<QuestionAnswer>();

        /// <summary>
        /// Challenge question with answer
        /// </summary>
        public QuestionAnswer? ChallengeQuestion { get; set; }

        /// <summary>
        /// Big question for the lesson
        /// </summary>
        public string? BigQuestion { get; set; }

        /// <summary>
        /// Starter activity
        /// </summary>
        public string? StarterActivity { get; set; }

        /// <summary>
        /// Main activity
        /// </summary>
        public string? MainActivity { get; set; }

        /// <summary>
        /// Plenary activity
        /// </summary>
        public string? PlenaryActivity { get; set; }
    }

    /// <summary>
    /// Represents a question and its answer
    /// </summary>
    public class QuestionAnswer
    {
        /// <summary>
        /// The question
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The answer
        /// </summary>
        public string Answer { get; set; } = string.Empty;
    }
}
