namespace Application.Dto
{
    public class MilestoneDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<SectionDto> Sections { get; set; } = new();
    }
}
