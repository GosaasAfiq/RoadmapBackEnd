namespace Application.Dto
{
    public class RoadmapDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } // Include if needed for the client
        public string RoadmapName { get; set; }
        public bool IsPublished { get; set; }
        public bool IsCompleted { get; set; } // Optional, depending on frontend requirements
        public string CreatedAt { get; set; } // Optional
        public string UpdatedAt { get; set; } // Optional
        public DateTime UpdatedAtRaw { get; set; }
        public DateTime CreatedAtRaw { get; set; }
        public double CompletionRate { get; set; } // Add completion rate property
        public string StartDate { get; set; } // Nullable in case no milestones exist
        public string EndDate { get; set; }
        public List<NodeDto> Nodes { get; set; } = new();
    }
}
