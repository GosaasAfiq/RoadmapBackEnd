namespace Application.Dto
{
    public class NodeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid RoadmapId { get; set; }
        public Guid? ParentId { get; set; } // Include if needed
        public DateTime CreatedAt { get; set; } // Optional
        public DateTime UpdatedAt { get; set; } // Optional
        public List<NodeDto> Children { get; set; } = new();
    }
}
