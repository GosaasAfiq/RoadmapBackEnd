using System.Text.Json.Serialization;

namespace Application.Dto
{
    public class SectionDto
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? StartDate { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? EndDate { get; set; }
        public List<SubSectionDto> SubSections { get; set; } = new();
    }
}
