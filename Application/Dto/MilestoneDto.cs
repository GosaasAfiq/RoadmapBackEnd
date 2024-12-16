using System.Text.Json.Serialization;

namespace Application.Dto
{
    public class MilestoneDto
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? StartDate { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? EndDate { get; set; }
        public List<SectionDto> Sections { get; set; } = new();
    }
}
