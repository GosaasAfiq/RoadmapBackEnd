namespace Application.Dto
{
    public class SectionDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<SubSectionDto> SubSections { get; set; } = new();
    }
}
