using Domain;
using Microsoft.EntityFrameworkCore;

namespace Persistence
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<AuditTrail> AuditTrail { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Roadmap> Roadmap { get; set; }
        public DbSet<Node> Node { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // User and AuditTrail Relationship
            builder.Entity<AuditTrail>()
                .HasOne(a => a.User) // Navigation property in AuditTrail
                .WithMany() // Navigation property in User
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User and Roadmap Relationship
            builder.Entity<Roadmap>()
                .HasOne(r => r.User) // Navigation property in Roadmap
                .WithMany(u => u.Roadmaps) // Navigation property in User
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Roadmap and Node Relationship
            builder.Entity<Node>()
                .HasOne(n => n.Roadmap) // Navigation property in Node
                .WithMany(r => r.Nodes) // Navigation property in Roadmap
                .HasForeignKey(n => n.RoadmapId)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing Node Relationship (Parent-Child)
            builder.Entity<Node>()
                .HasOne(n => n.Parent) // Navigation property in Node
                .WithMany(n => n.Children) // Navigation property in Node
                .HasForeignKey(n => n.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletions to avoid orphaned nodes

        }
    }
}