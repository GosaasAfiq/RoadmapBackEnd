namespace Application.Dto
{
    public class RoadmapDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } // Include if needed for the client
        public string RoadmapName { get; set; }
        public bool IsPublished { get; set; }
        public bool IsCompleted { get; set; } // Optional, depending on frontend requirements
        public DateTime CreatedAt { get; set; } // Optional
        public DateTime UpdatedAt { get; set; } // Optional
        public double CompletionRate { get; set; } // Add completion rate property
        public string StartDate { get; set; } // Nullable in case no milestones exist
        public string EndDate { get; set; }
        public List<NodeDto> Nodes { get; set; } = new();
    }
}
