using Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Application.Dto;

namespace Application.Roadmaps
{
    public class Details
    {
        public class Query : IRequest<RoadmapDto> // Return RoadmapDto instead of Roadmap
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Query, RoadmapDto>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<RoadmapDto> Handle(Query request, CancellationToken cancellationToken)
            {
                // Fetch the roadmap including its nodes and children
                var roadmap = await _context.Roadmap
                    .Include(r => r.Nodes.OrderBy(n => n.CreateAt)) // Sort top-level nodes by CreatedAt
                        .ThenInclude(n => n.Children.OrderBy(c => c.CreateAt)) // Sort second-level nodes by CreatedAt
                        .ThenInclude(c => c.Children.OrderBy(cc => cc.CreateAt)) // Sort third-level nodes by CreatedAt
                    .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

                if (roadmap == null)
                {
                    return null; // Or handle differently if you want to return an error response
                }

                var startDate = roadmap.Nodes
                    .Where(n => n.ParentId == null)
                    .OrderBy(n => n.StartDate)
                    .Select(n => n.StartDate.HasValue ? n.StartDate.Value.AddDays(1).Date.ToString("dd-MM-yyyy") : null)
                    .FirstOrDefault();

                var endDate = roadmap.Nodes
                    .Where(n => n.ParentId == null)
                    .OrderByDescending(n => n.EndDate)
                    .Select(n => n.EndDate.HasValue ? n.EndDate.Value.AddDays(1).Date.ToString("dd-MM-yyyy") : null)
                    .FirstOrDefault();

                //var completionRate = CalculateCompletionRate(roadmap);

                // Map the roadmap to a DTO with child nodes
                var roadmapDto = new RoadmapDto
                {
                    Id = roadmap.Id,
                    UserId = roadmap.UserId,
                    RoadmapName = roadmap.RoadmapName,
                    IsPublished = roadmap.IsPublished,
                    IsCompleted = roadmap.IsCompleted,
                    CreatedAt = roadmap.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = roadmap.UpdatedAt.ToString("dd-MM-yyyy"),
                    StartDate = startDate,
                    EndDate = endDate,
                    CompletionRate = CalculateCompletionRate(roadmap),
                    Nodes = roadmap.Nodes
                        .Where(n => n.ParentId == null) // Only include root nodes
                        .OrderBy(n => n.CreateAt) // Sort root nodes by CreatedAt
                        .Select(MapNodeToDto) // Recursively map child nodes
                        .ToList()
                };

                return roadmapDto;
            }

            private double CalculateCompletionRate(Roadmap roadmap)
            {
                var milestones = roadmap.Nodes.Where(n => n.ParentId == null).ToList(); // Top-level milestones
                var totalMilestones = milestones.Count;

                if (totalMilestones == 0)
                {
                    return 0;
                }

                double totalCompletionRate = 0;
                foreach (var milestone in milestones)
                {
                    totalCompletionRate += CalculateMilestoneCompletionRate(milestone);
                }

                return Math.Round((totalCompletionRate / totalMilestones), 2); // Return average completion rate of all milestones
            }


            private double CalculateMilestoneCompletionRate(Node milestone)
            {
                var sections = milestone.Children.ToList(); // Sections under the milestone
                var totalSections = sections.Count;

                // If no sections, check if the milestone itself is marked as completed
                if (totalSections == 0)
                {
                    return milestone.IsCompleted ? 100 : 0;
                }

                double totalSectionCompletion = 0;
                foreach (var section in sections)
                {
                    totalSectionCompletion += CalculateSectionCompletionRate(section);
                }

                return Math.Round((totalSectionCompletion / totalSections), 2); // Return average completion rate of all sections
            }


            private double CalculateSectionCompletionRate(Node section)
            {
                var subsections = section.Children.ToList(); // Subsections under the section
                var totalSubsections = subsections.Count;

                // If no subsections, check if the section itself is marked as completed
                if (totalSubsections == 0)
                {
                    return section.IsCompleted ? 100 : 0;
                }

                double totalSubsectionCompletion = 0;
                foreach (var subsection in subsections)
                {
                    totalSubsectionCompletion += subsection.IsCompleted ? 100 : 0; // 100 if completed, 0 if not
                }

                return Math.Round((totalSubsectionCompletion / totalSubsections), 2); // Return average completion rate of all subsections
            }




            private NodeDto MapNodeToDto(Node node)
            {
                return new NodeDto
                {
                    Id = node.Id,
                    RoadmapId = node.RoadmapId,
                    ParentId = node.ParentId,
                    Name = node.Name,
                    Description = node.Description,
                    IsCompleted = node.IsCompleted,
                    StartDate = node.StartDate,
                    EndDate = node.EndDate,
                    CreateAt = node.CreateAt,
                    UpdatedAt = node.UpdatedAt,
                    CompletionRate = CalculateNodeCompletionRate(node),
                    Children = node.Children
                        .OrderBy(c => c.CreateAt)
                        .Select(MapNodeToDto) // Recursively map child nodes
                        .ToList()
                };
            }

            private double CalculateNodeCompletionRate(Node node)
            {
                if (node.ParentId == null) // It's a milestone
                {
                    return CalculateMilestoneCompletionRate(node);
                }
                else if (node.Children.Count > 0) // It's a section (has subsections)
                {
                    return CalculateSectionCompletionRate(node);
                }
                else // It's a subsection
                {
                    return node.IsCompleted ? 100 : 0;
                }
            }
        }
    }
}

