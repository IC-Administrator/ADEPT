using System.Text.Json;

namespace Adept.Common.Json
{
    /// <summary>
    /// Helper class for JSON serialization and deserialization
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Serializes an object to a JSON string
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="value">The object to serialize</param>
        /// <param name="options">The serialization options (optional)</param>
        /// <returns>The JSON string</returns>
        public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(value, options ?? JsonSerializerOptionsFactory.Default);
        }

        /// <summary>
        /// Serializes an object to a JSON string with indented formatting
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="value">The object to serialize</param>
        /// <returns>The indented JSON string</returns>
        public static string SerializeIndented<T>(T value)
        {
            return JsonSerializer.Serialize(value, JsonSerializerOptionsFactory.Indented);
        }

        /// <summary>
        /// Deserializes a JSON string to an object
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>The deserialized object</returns>
        /// <exception cref="JsonException">Thrown when deserialization fails</exception>
        public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, options ?? JsonSerializerOptionsFactory.Default);
        }

        /// <summary>
        /// Tries to deserialize a JSON string to an object
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="result">The deserialized object (output)</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        public static bool TryDeserialize<T>(string json, out T? result, JsonSerializerOptions? options = null)
        {
            result = default;

            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                result = JsonSerializer.Deserialize<T>(json, options ?? JsonSerializerOptionsFactory.Default);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a string is valid JSON
        /// </summary>
        /// <param name="json">The JSON string to validate</param>
        /// <returns>True if the string is valid JSON, false otherwise</returns>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                using (JsonDocument.Parse(json))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a string is valid JSON and can be deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to validate</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if the string is valid JSON and can be deserialized to the specified type, false otherwise</returns>
        public static bool IsValidJson<T>(string json, JsonSerializerOptions? options = null)
        {
            return TryDeserialize<T>(json, out _, options);
        }

        /// <summary>
        /// Validates that a string is valid JSON and can be deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to validate</param>
        /// <param name="validationErrors">The validation errors (output)</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if the string is valid JSON and can be deserialized to the specified type, false otherwise</returns>
        public static bool ValidateJson<T>(string json, out List<string> validationErrors, JsonSerializerOptions? options = null)
        {
            validationErrors = new List<string>();

            if (string.IsNullOrEmpty(json))
            {
                validationErrors.Add("JSON string is null or empty");
                return false;
            }

            try
            {
                using (JsonDocument.Parse(json))
                {
                    try
                    {
                        JsonSerializer.Deserialize<T>(json, options ?? JsonSerializerOptionsFactory.Default);
                        return true;
                    }
                    catch (JsonException ex)
                    {
                        validationErrors.Add($"JSON deserialization error: {ex.Message}");
                        return false;
                    }
                }
            }
            catch (JsonException ex)
            {
                validationErrors.Add($"JSON parsing error: {ex.Message}");
                return false;
            }
        }
    }
}
