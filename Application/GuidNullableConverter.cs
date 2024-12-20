using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application
{
    public class GuidNullableConverter : JsonConverter<Guid?>
    {
        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null; // Treat empty string as null
                }

                if (Guid.TryParse(stringValue, out var guidValue))
                {
                    return guidValue; // Parse valid GUIDs
                }
            }

            return null; // Fallback to null for other cases
        }

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString());
        }
    }

}
