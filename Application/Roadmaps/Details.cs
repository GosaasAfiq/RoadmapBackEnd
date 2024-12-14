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
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<RoadmapDto> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching details for roadmap with ID: {RoadmapId}", request.Id);

                // Fetch the roadmap including its nodes and children
                var roadmap = await _context.Roadmap
                    .Include(r => r.Nodes.OrderBy(n => n.CreateAt)) // Sort top-level nodes by CreatedAt
                        .ThenInclude(n => n.Children.OrderBy(c => c.CreateAt)) // Sort second-level nodes by CreatedAt
                        .ThenInclude(c => c.Children.OrderBy(cc => cc.CreateAt)) // Sort third-level nodes by CreatedAt
                    .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

                if (roadmap == null)
                {
                    _logger.LogWarning("Roadmap with ID: {RoadmapId} not found", request.Id);
                    return null; // Or handle differently if you want to return an error response
                }

                _logger.LogInformation("Successfully fetched roadmap with ID: {RoadmapId}", request.Id);

                // Map the roadmap to a DTO with child nodes
                var roadmapDto = new RoadmapDto
                {
                    Id = roadmap.Id,
                    UserId = roadmap.UserId,
                    RoadmapName = roadmap.RoadmapName,
                    IsPublished = roadmap.IsPublished,
                    IsCompleted = roadmap.IsCompleted,
                    CreatedAt = roadmap.CreatedAt,
                    UpdatedAt = roadmap.UpdatedAt,
                    Nodes = roadmap.Nodes
                        .Where(n => n.ParentId == null) // Only include root nodes
                        .OrderBy(n => n.CreateAt) // Sort root nodes by CreatedAt
                        .Select(MapNodeToDto) // Recursively map child nodes
                        .ToList()
                };

                return roadmapDto;
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
                    Children = node.Children
                        .OrderBy(c => c.CreateAt)
                        .Select(MapNodeToDto) // Recursively map child nodes
                        .ToList()
                };
            }
        }
    }
}


//private double CalculateCompletionRate(Roadmap roadmap)
//{
//    var totalMilestones = roadmap.Nodes.Count;
//    var completedMilestones = roadmap.Nodes.Count(n => CalculateMilestoneCompletionRate(n) == 100);

//    if (totalMilestones > 0)
//    {
//        double averageMilestoneCompletion = roadmap.Nodes.Average(n => CalculateMilestoneCompletionRate(n));
//        return Math.Round(averageMilestoneCompletion, 2);
//    }
//    return 0;
//}


//private double CalculateMilestoneCompletionRate(Node milestone)
//{
//    var totalSections = milestone.Children.Count;
//    var completedSections = milestone.Children.Count(s => CalculateSectionCompletionRate(s) == 100);

//    // Calculate the completion rate based on average completion of sections
//    if (totalSections > 0)
//    {
//        double averageSectionCompletion = milestone.Children.Average(s => CalculateSectionCompletionRate(s));
//        return Math.Round(averageSectionCompletion, 2);
//    }
//    return 0;
//}


//private double CalculateSectionCompletionRate(Node section)
//{
//    var totalSubsections = section.Children.Count;
//    var completedSubsections = section.Children.Count(c => c.IsCompleted);

//    return totalSubsections > 0
//        ? Math.Round((double)completedSubsections / totalSubsections * 100, 2)
//        : 0;
//}