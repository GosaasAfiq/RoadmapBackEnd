using System.Text.Json.Serialization;

namespace Application.Dto
{
    public class SectionDto
    {
        [JsonConverter(typeof(GuidNullableConverter))]
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? StartDate { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? EndDate { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? CreateAt { get; set; }
        public List<SubSectionDto> SubSections { get; set; } = new();
    }
}
