namespace Domain
{
    public class Node
    {
        public Guid Id { get; set; }
        public Guid RoadmapId { get; set; }
        public Guid? ParentId { get; set; } // Nullable for root nodes
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property for Roadmap
        public Roadmap Roadmap { get; set; }

        // Navigation property for Parent Node
        public Node Parent { get; set; }

        // Navigation property for Child Nodes
        public ICollection<Node> Children { get; set; }
    }
}
