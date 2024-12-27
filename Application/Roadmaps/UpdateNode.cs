using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Roadmaps
{
    public class UpdateNode
    {
        public class Command : IRequest
        {
            public Roadmap Roadmap { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly DataContext _context;
            public Handler(DataContext context)
            {
                _context = context;
            }


            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var existingRoadmap = await _context.Roadmap
                    .Include(r => r.Nodes)
                    .FirstOrDefaultAsync(r => r.Id == request.Roadmap.Id, cancellationToken);

                foreach (var incomingNode in request.Roadmap.Nodes)
                {
                    var existingNode = existingRoadmap.Nodes.FirstOrDefault(n => n.Id == incomingNode.Id);

                    // Compare the existing node and update isCompleted if conditions are met
                    if (existingNode != null && incomingNode.IsCompleted && !existingNode.IsCompleted)
                    {
                        existingNode.IsCompleted = true;
                        existingNode.UpdatedAt = DateTime.UtcNow; // Update the timestamp
                    }
                }

                foreach (var node in existingRoadmap.Nodes)
                {
                    UpdateSectionNode(node);
                }

                foreach (var node in existingRoadmap.Nodes)
                {
                    UpdateMilestoneNode(node);
                }

                // Now check if all milestones are complete to mark the roadmap as complete
                UpdateRoadmapStatus(existingRoadmap);

                // Save the changes
                await _context.SaveChangesAsync(cancellationToken);
            }
            private void UpdateSectionNode(Node node)
            {

                if (node.ParentId != null)
                {
                    ProcessSectionNode(node);
                }
            }

            private void UpdateMilestoneNode(Node node)
            {

                if (node.ParentId == null)
                {
                   ProcessMilestoneNode(node);
                }

            }

            private void ProcessSectionNode(Node node)
            {
                // Step 2: Check if the section node has children
                if (node.Children != null && node.Children.Any())
                {
                    // Step 3: Put all the children in a list and check if all are completed
                    List<Node> children = node.Children.ToList();
                    bool allChildrenCompleted = true;

                    foreach (var child in children)
                    {
                        if (!child.IsCompleted)
                        {
                            allChildrenCompleted = false;
                            break;
                        }
                    }

                    // Step 4: If all children are completed, mark the section as completed
                    if (allChildrenCompleted)
                    {
                        node.IsCompleted = true;
                        node.UpdatedAt = DateTime.UtcNow;
                        _context.Node.Update(node);
                    } 
                }                
            }

            private void ProcessMilestoneNode(Node node)
            {
                // Step 5: Now check if the milestone node has children
                if (node.Children != null && node.Children.Any())
                {
                    // Step 6: Put all children in a list and check if all of them are completed
                    List<Node> children = node.Children.ToList();
                    bool allChildrenCompleted = true;

                    foreach (var child in children)
                    {
                        if (!child.IsCompleted)
                        {
                            allChildrenCompleted = false;
                            break;
                        }
                    }

                    // Step 7: If all children are completed, mark the milestone as completed
                    if (allChildrenCompleted)
                    {
                        node.IsCompleted = true;
                        node.UpdatedAt = DateTime.UtcNow;
                        _context.Node.Update(node);
                    }   
                }
            }


            private void UpdateRoadmapStatus(Roadmap roadmap)
            {
                // Check if all milestones are complete
                var allMilestonesComplete = roadmap.Nodes
                    .Where(n => n.ParentId == null) // Milestones only
                    .All(m => m.IsCompleted);

                if (allMilestonesComplete && !roadmap.IsCompleted)
                {
                    roadmap.IsCompleted = true;
                    roadmap.UpdatedAt = DateTime.UtcNow;
                }
            }

        }
    }
}

