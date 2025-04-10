using System.Text.Json;

namespace Adept.Common.Json
{
    /// <summary>
    /// Extension methods for JSON serialization and deserialization
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Serializes an object to a JSON string
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="value">The object to serialize</param>
        /// <param name="options">The serialization options (optional)</param>
        /// <returns>The JSON string</returns>
        public static string ToJson<T>(this T value, JsonSerializerOptions? options = null)
        {
            return JsonHelper.Serialize(value, options);
        }

        /// <summary>
        /// Serializes an object to a JSON string with indented formatting
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="value">The object to serialize</param>
        /// <returns>The indented JSON string</returns>
        public static string ToIndentedJson<T>(this T value)
        {
            return JsonHelper.SerializeIndented(value);
        }

        /// <summary>
        /// Deserializes a JSON string to an object
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>The deserialized object</returns>
        /// <exception cref="JsonException">Thrown when deserialization fails</exception>
        public static T? FromJson<T>(this string json, JsonSerializerOptions? options = null)
        {
            return JsonHelper.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Tries to deserialize a JSON string to an object
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="result">The deserialized object (output)</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        public static bool TryFromJson<T>(this string json, out T? result, JsonSerializerOptions? options = null)
        {
            return JsonHelper.TryDeserialize(json, out result, options);
        }

        /// <summary>
        /// Validates that a string is valid JSON
        /// </summary>
        /// <param name="json">The JSON string to validate</param>
        /// <returns>True if the string is valid JSON, false otherwise</returns>
        public static bool IsValidJson(this string json)
        {
            return JsonHelper.IsValidJson(json);
        }

        /// <summary>
        /// Validates that a string is valid JSON and can be deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to validate</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if the string is valid JSON and can be deserialized to the specified type, false otherwise</returns>
        public static bool IsValidJson<T>(this string json, JsonSerializerOptions? options = null)
        {
            return JsonHelper.IsValidJson<T>(json, options);
        }

        /// <summary>
        /// Validates that a string is valid JSON and can be deserialized to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to validate</param>
        /// <param name="validationErrors">The validation errors (output)</param>
        /// <param name="options">The deserialization options (optional)</param>
        /// <returns>True if the string is valid JSON and can be deserialized to the specified type, false otherwise</returns>
        public static bool ValidateJson<T>(this string json, out List<string> validationErrors, JsonSerializerOptions? options = null)
        {
            return JsonHelper.ValidateJson<T>(json, out validationErrors, options);
        }

        /// <summary>
        /// Clones an object by serializing and deserializing it
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="source">The object to clone</param>
        /// <param name="options">The serialization options (optional)</param>
        /// <returns>A deep clone of the object</returns>
        public static T? DeepClone<T>(this T source, JsonSerializerOptions? options = null)
        {
            if (source == null)
            {
                return default;
            }

            var json = JsonHelper.Serialize(source, options);
            return JsonHelper.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Converts an object to another type by serializing and deserializing it
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TTarget">The target type</typeparam>
        /// <param name="source">The object to convert</param>
        /// <param name="options">The serialization options (optional)</param>
        /// <returns>The converted object</returns>
        public static TTarget? ConvertTo<TSource, TTarget>(this TSource source, JsonSerializerOptions? options = null)
        {
            if (source == null)
            {
                return default;
            }

            var json = JsonHelper.Serialize(source, options);
            return JsonHelper.Deserialize<TTarget>(json, options);
        }
    }
}
