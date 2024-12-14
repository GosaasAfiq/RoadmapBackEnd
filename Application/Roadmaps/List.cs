using Application.Dto;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.Roadmaps
{
    public class List
    {
        public class Query : IRequest<List<RoadmapDto>> 
        {
            public Guid UserId { get; set; }
            public string SearchTerm { get; set; } // Optional search term
            public string Filter { get; set; } = "all"; // Default filter is 'all'

        }

        public class Handler : IRequestHandler<Query, List<RoadmapDto>>
        {
            private readonly DataContext _context;
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<List<RoadmapDto>> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching all roadmaps.");

                var query = _context.Roadmap
                    .Where(r => r.UserId == request.UserId)
                    .Include(r => r.Nodes.OrderBy(n => n.CreateAt)) // Sort top-level nodes by CreatedAt
                        .ThenInclude(n => n.Children.OrderBy(c => c.CreateAt)) // Sort second-level nodes by CreatedAt
                        .ThenInclude(c => c.Children.OrderBy(cc => cc.CreateAt)) // Sort third-level nodes by CreatedAt
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r =>
                        EF.Functions.Like(r.RoadmapName.ToLower(), $"%{request.SearchTerm.ToLower()}%"));
                }

                switch (request.Filter.ToLower())
                {
                    case "draft":
                        query = query.Where(r => !r.IsPublished);
                        break;
                    case "not-started":
                        query = query.Where(r => r.IsPublished && !r.IsCompleted && !r.Nodes.Any(n => n.IsCompleted));
                        break;
                    case "in-progress":
                        query = query.Where(r => r.IsPublished && !r.IsCompleted && r.Nodes.Any(n => n.IsCompleted));
                        break;
                    case "completed":
                        query = query.Where(r => r.IsCompleted);
                        break;
                    case "all":
                    default:
                        // No filter, show all
                        break;
                }

                var roadmaps = await query.ToListAsync(cancellationToken);


                if (roadmaps.Count == 0)
                {
                    _logger.LogWarning("No roadmaps found in the database.");
                }
                else
                {
                    _logger.LogInformation("Successfully fetched {Count} roadmaps.", roadmaps.Count);
                }

                var roadmapDtos = roadmaps.Select(r => new RoadmapDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    RoadmapName = r.RoadmapName,
                    IsPublished = r.IsPublished,
                    IsCompleted = r.IsCompleted,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CompletionRate = CalculateCompletionRate(r),
                    Nodes = r.Nodes
                        .Where(n => n.ParentId == null) // Only include root nodes
                        .OrderBy(n => n.CreateAt) // Sort root nodes by CreatedAt
                        .Select(MapNodeToDto) // Map nodes recursively
                        .ToList()
                }).ToList();

                return roadmapDtos;
            }

            private double CalculateCompletionRate(Roadmap roadmap)
            {
                // Get all milestones (parent nodes with null ParentId)
                var milestones = roadmap.Nodes.Where(n => n.ParentId == null).ToList();

                // Calculate the completion rate of each milestone
                var completedMilestones = milestones.Count(m => CalculateMilestoneCompletionRate(m) == 100);

                if (milestones.Count > 0)
                {
                    double averageMilestoneCompletion = milestones.Average(m => CalculateMilestoneCompletionRate(m));
                    return Math.Round(averageMilestoneCompletion, 2);
                }

                return 0;
            }

            private double CalculateMilestoneCompletionRate(Node milestone)
            {
                // Get all sections (child nodes of the milestone)
                var sections = milestone.Children.ToList();

                // Calculate the completion rate of each section
                var sectionCompletionRates = sections.Select(s => CalculateSectionCompletionRate(s)).ToList();

                if (sections.Count > 0)
                {
                    double averageSectionCompletion = sectionCompletionRates.Average();
                    return Math.Round(averageSectionCompletion, 2);
                }

                return 0; // If no sections, the milestone is considered 0% complete
            }

            private double CalculateSectionCompletionRate(Node section)
            {
                // Get all subsections (child nodes of the section)
                var subsections = section.Children.ToList();

                // Calculate the completion rate of each subsection
                var subsectionCompletionRates = subsections.Select(s => s.IsCompleted ? 100 : 0).ToList();

                if (subsections.Count > 0)
                {
                    double averageSubsectionCompletion = subsectionCompletionRates.Average();
                    return Math.Round(averageSubsectionCompletion, 2);
                }

                return 0; // If no subsections, the section is considered 0% complete
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
                    CreatedAt = node.CreateAt,
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
