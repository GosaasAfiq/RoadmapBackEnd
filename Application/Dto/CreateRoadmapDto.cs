using System.Text.Json.Serialization;

namespace Application.Dto
{
    public class CreateRoadmapDto
    {
        [JsonConverter(typeof(GuidNullableConverter))]
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public Guid UserId { get; set; }
        public bool IsPublished { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? CreatedAt { get; set; }

        public List<MilestoneDto> Milestones { get; set; } = new();
    }
}
