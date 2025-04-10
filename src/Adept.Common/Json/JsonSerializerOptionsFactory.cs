using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adept.Common.Json
{
    /// <summary>
    /// Factory for creating standardized JsonSerializerOptions
    /// </summary>
    public static class JsonSerializerOptionsFactory
    {
        /// <summary>
        /// Gets the default JsonSerializerOptions for the application
        /// </summary>
        public static JsonSerializerOptions Default => new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Gets JsonSerializerOptions with indented output
        /// </summary>
        public static JsonSerializerOptions Indented => new JsonSerializerOptions(Default)
        {
            WriteIndented = true
        };

        /// <summary>
        /// Gets JsonSerializerOptions for API requests
        /// </summary>
        public static JsonSerializerOptions ApiRequest => new JsonSerializerOptions(Default)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Gets JsonSerializerOptions for API responses
        /// </summary>
        public static JsonSerializerOptions ApiResponse => new JsonSerializerOptions(Default)
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Gets JsonSerializerOptions for file storage
        /// </summary>
        public static JsonSerializerOptions FileStorage => new JsonSerializerOptions(Indented)
        {
            WriteIndented = true
        };

        /// <summary>
        /// Creates a custom JsonSerializerOptions based on the default options
        /// </summary>
        /// <param name="configure">Action to configure the options</param>
        /// <returns>The configured JsonSerializerOptions</returns>
        public static JsonSerializerOptions Create(Action<JsonSerializerOptions> configure)
        {
            var options = new JsonSerializerOptions(Default);
            configure(options);
            return options;
        }
    }
}
