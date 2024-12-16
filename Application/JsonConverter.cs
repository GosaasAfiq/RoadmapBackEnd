using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application
{
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                return string.IsNullOrWhiteSpace(dateString) ? (DateTime?)null : DateTime.Parse(dateString);
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("o"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

}
