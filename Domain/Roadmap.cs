namespace Domain
{
    public class Roadmap
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string RoadmapName { get; set; }
        public bool IsPublished { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property to User
        public User User { get; set; }

        // Navigation property for related Nodes
        public ICollection<Node> Nodes { get; set; }
    }
}
