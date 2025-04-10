using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Adept.Common.Json
{
    /// <summary>
    /// Validates that a string is valid JSON
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValidJsonAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidJsonAttribute"/> class
        /// </summary>
        public ValidJsonAttribute()
            : base("The {0} field is not valid JSON.")
        {
        }

        /// <summary>
        /// Validates the specified value
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="validationContext">The validation context</param>
        /// <returns>The validation result</returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is not string json)
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a string.");
            }

            if (string.IsNullOrEmpty(json))
            {
                return ValidationResult.Success;
            }

            try
            {
                using (JsonDocument.Parse(json))
                {
                    return ValidationResult.Success;
                }
            }
            catch (JsonException ex)
            {
                return new ValidationResult($"The {validationContext.DisplayName} field is not valid JSON: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Validates that a string is valid JSON and can be deserialized to the specified type
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ValidJsonObjectAttribute<T> : ValidationAttribute
    {
        private readonly JsonSerializerOptions? _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidJsonObjectAttribute{T}"/> class
        /// </summary>
        public ValidJsonObjectAttribute()
            : base($"The {{0}} field is not valid JSON for type {typeof(T).Name}.")
        {
            _options = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidJsonObjectAttribute{T}"/> class
        /// </summary>
        /// <param name="useDefaultOptions">Whether to use the default options</param>
        public ValidJsonObjectAttribute(bool useDefaultOptions)
            : base($"The {{0}} field is not valid JSON for type {typeof(T).Name}.")
        {
            _options = useDefaultOptions ? JsonSerializerOptionsFactory.Default : null;
        }

        /// <summary>
        /// Validates the specified value
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="validationContext">The validation context</param>
        /// <returns>The validation result</returns>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is not string json)
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must be a string.");
            }

            if (string.IsNullOrEmpty(json))
            {
                return ValidationResult.Success;
            }

            if (!JsonHelper.ValidateJson<T>(json, out var errors, _options))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field is not valid JSON for type {typeof(T).Name}: {string.Join(", ", errors)}");
            }

            return ValidationResult.Success;
        }
    }
}
