using System.Text.Json;

namespace Adept.Common.Json
{
    /// <summary>
    /// Exception thrown when JSON validation fails
    /// </summary>
    public class JsonValidationException : Exception
    {
        /// <summary>
        /// Gets the validation errors
        /// </summary>
        public List<string> ValidationErrors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonValidationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public JsonValidationException(string message)
            : base(message)
        {
            ValidationErrors = new List<string> { message };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonValidationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public JsonValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            ValidationErrors = new List<string> { message };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonValidationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="validationErrors">The validation errors</param>
        public JsonValidationException(string message, List<string> validationErrors)
            : base(message)
        {
            ValidationErrors = validationErrors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonValidationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="validationErrors">The validation errors</param>
        /// <param name="innerException">The inner exception</param>
        public JsonValidationException(string message, List<string> validationErrors, Exception innerException)
            : base(message, innerException)
        {
            ValidationErrors = validationErrors;
        }
    }

    /// <summary>
    /// Exception thrown when JSON serialization fails
    /// </summary>
    public class JsonSerializationException : Exception
    {
        /// <summary>
        /// Gets the type that failed to serialize
        /// </summary>
        public Type? SourceType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public JsonSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public JsonSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
            // JsonException doesn't have a SourceType property
            // We'll need to extract the type information from the message or other means if needed
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="sourceType">The type that failed to serialize</param>
        public JsonSerializationException(string message, Type sourceType)
            : base(message)
        {
            SourceType = sourceType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="sourceType">The type that failed to serialize</param>
        /// <param name="innerException">The inner exception</param>
        public JsonSerializationException(string message, Type sourceType, Exception innerException)
            : base(message, innerException)
        {
            SourceType = sourceType;
        }
    }

    /// <summary>
    /// Exception thrown when JSON deserialization fails
    /// </summary>
    public class JsonDeserializationException : Exception
    {
        /// <summary>
        /// Gets the type that failed to deserialize
        /// </summary>
        public Type? TargetType { get; }

        /// <summary>
        /// Gets the JSON that failed to deserialize
        /// </summary>
        public string? Json { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public JsonDeserializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public JsonDeserializationException(string message, Exception innerException)
            : base(message, innerException)
        {
            // JsonException doesn't have a SourceType property
            // We'll need to extract the type information from the message or other means if needed
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="targetType">The type that failed to deserialize</param>
        /// <param name="json">The JSON that failed to deserialize</param>
        public JsonDeserializationException(string message, Type targetType, string json)
            : base(message)
        {
            TargetType = targetType;
            Json = json;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDeserializationException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="targetType">The type that failed to deserialize</param>
        /// <param name="json">The JSON that failed to deserialize</param>
        /// <param name="innerException">The inner exception</param>
        public JsonDeserializationException(string message, Type targetType, string json, Exception innerException)
            : base(message, innerException)
        {
            TargetType = targetType;
            Json = json;
        }
    }
}
