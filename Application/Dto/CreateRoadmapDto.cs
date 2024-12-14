namespace Application.Dto
{
    public class CreateRoadmapDto
    {
        public string Name { get; set; }
        public Guid UserId { get; set; }
        public bool IsPublished { get; set; }

        public List<MilestoneDto> Milestones { get; set; } = new();
    }
}
