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
        public class Query : IRequest<Result>
        {
            public Guid UserId { get; set; }
            public string SearchTerm { get; set; } // Optional search term
            public string Filter { get; set; } = "all"; // Default filter is 'all'
            public int Page { get; set; } = 1; // Default to page 1
            public int PageSize { get; set; } = 6; // Default to 6 items per page
        }

        public class Result
        {
            public int TotalCount { get; set; } // Total count of roadmaps (before pagination)
            public List<RoadmapDto> Items { get; set; } // Paginated roadmaps
            public int DraftCount { get; set; }
            public int NotStartedCount { get; set; }
            public int InProgressCount { get; set; }
            public int CompletedCount { get; set; }
            public int NearDueCount { get; set; } // Count of near due roadmaps
            public int OverdueCount { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly DataContext _context;
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching all roadmaps.");
                var currentDate = DateTime.UtcNow; // Use UTC or DateTime.Now based on your requirements


                // Start the query
                var query = _context.Roadmap
                    .Where(r => r.UserId == request.UserId)
                    .Include(r => r.Nodes.OrderBy(n => n.CreateAt)) // Sort top-level nodes by CreatedAt
                        .ThenInclude(n => n.Children.OrderBy(c => c.CreateAt)) // Sort second-level nodes by CreatedAt
                        .ThenInclude(c => c.Children.OrderBy(cc => cc.CreateAt)) // Sort third-level nodes by CreatedAt
                    .AsQueryable();

                // Apply search term filter if provided
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r =>
                        EF.Functions.Like(r.RoadmapName.ToLower(), $"%{request.SearchTerm.ToLower()}%"));
                }

                var draftCount = await query.CountAsync(r => !r.IsPublished, cancellationToken);
                var notStartedCount = await query.CountAsync(r => r.IsPublished && !r.IsCompleted && !r.Nodes.Any(n => n.IsCompleted), cancellationToken);
                var inProgressCount = await query.CountAsync(r => r.IsPublished && !r.IsCompleted && r.Nodes.Any(n => n.IsCompleted), cancellationToken);
                var completedCount = await query.CountAsync(r => r.IsCompleted, cancellationToken);
                var nearDueCount = await query.CountAsync(r =>
                    r.Nodes.Where(n => n.ParentId == null)
                           .OrderByDescending(n => n.EndDate)
                           .Select(n => n.EndDate)
                           .FirstOrDefault() > currentDate &&
                    r.Nodes.Where(n => n.ParentId == null)
                           .OrderByDescending(n => n.EndDate)
                           .Select(n => n.EndDate)
                           .FirstOrDefault() <= currentDate.AddDays(5),
                    cancellationToken);
                var overdueCount = await query.CountAsync(r =>
                    r.Nodes.Where(n => n.ParentId == null)
                           .OrderByDescending(n => n.EndDate)
                           .Select(n => n.EndDate)
                           .FirstOrDefault() < currentDate,
                    cancellationToken);


                // Apply filter based on the provided value (draft, completed, etc.)
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
                    case "near-due":
                        query = query.Where(r =>
                            r.Nodes.Where(n => n.ParentId == null)
                                   .OrderByDescending(n => n.EndDate)
                                   .Select(n => n.EndDate)
                                   .FirstOrDefault() > currentDate &&
                            r.Nodes.Where(n => n.ParentId == null)
                                   .OrderByDescending(n => n.EndDate)
                                   .Select(n => n.EndDate)
                                   .FirstOrDefault() <= currentDate.AddDays(5));
                        break;
                    case "overdue":
                        query = query.Where(r =>
                            r.Nodes.Where(n => n.ParentId == null)
                                   .OrderByDescending(n => n.EndDate)
                                   .Select(n => n.EndDate)
                                   .FirstOrDefault() < currentDate);
                        break;
                    case "all":
                    default:
                        // No additional filter, show all
                        break;
                }

                // Get total count of roadmaps matching the query (before pagination)
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply pagination using Skip and Take
                var roadmaps = await query
                    .Skip((request.Page - 1) * request.PageSize)  // Skip the appropriate number of items
                    .Take(request.PageSize)  // Take the number of items specified in PageSize
                    .ToListAsync(cancellationToken);

                // Log the count of fetched roadmaps
                if (roadmaps.Count == 0)
                {
                    _logger.LogWarning("No roadmaps found in the database.");
                }
                else
                {
                    _logger.LogInformation("Successfully fetched {Count} roadmaps.", roadmaps.Count);
                }

                // Map the fetched roadmaps to RoadmapDto
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
                    StartDate = r.Nodes
                        .Where(n => n.ParentId == null) // Only consider milestones
                        .OrderBy(n => n.StartDate)
                        .Select(n => n.StartDate.Date.ToString("yyyy-MM-dd")) // Convert to string in "yyyy-MM-dd" format
                        .FirstOrDefault(),

                    EndDate = r.Nodes
                        .Where(n => n.ParentId == null) // Only consider milestones
                        .OrderByDescending(n => n.EndDate)
                        .Select(n => n.EndDate.Date.ToString("yyyy-MM-dd")) // Convert to string in "yyyy-MM-dd" format
                        .FirstOrDefault(),


                    Nodes = r.Nodes
                        .Where(n => n.ParentId == null) // Only include root nodes
                        .OrderBy(n => n.CreateAt) // Sort root nodes by CreatedAt
                        .Select(MapNodeToDto) // Map nodes recursively
                        .ToList()
                }).ToList();

                // Return the result with total count and paginated items
                return new Result
                {
                    TotalCount = totalCount,
                    Items = roadmapDtos,
                    DraftCount = draftCount,
                    NotStartedCount = notStartedCount,
                    InProgressCount = inProgressCount,
                    CompletedCount = completedCount,
                    NearDueCount = nearDueCount,
                    OverdueCount = overdueCount
                };
            }

            // Method to calculate the completion rate for roadmaps, milestones, sections, and subsections remains unchanged
            private double CalculateCompletionRate(Roadmap roadmap)
            {
                var milestones = roadmap.Nodes.Where(n => n.ParentId == null).ToList();
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
                var sections = milestone.Children.ToList();
                var sectionCompletionRates = sections.Select(s => CalculateSectionCompletionRate(s)).ToList();

                if (sections.Count > 0)
                {
                    double averageSectionCompletion = sectionCompletionRates.Average();
                    return Math.Round(averageSectionCompletion, 2);
                }

                return 0;
            }

            private double CalculateSectionCompletionRate(Node section)
            {
                var subsections = section.Children.ToList();
                var subsectionCompletionRates = subsections.Select(s => s.IsCompleted ? 100 : 0).ToList();

                if (subsections.Count > 0)
                {
                    double averageSubsectionCompletion = subsectionCompletionRates.Average();
                    return Math.Round(averageSubsectionCompletion, 2);
                }

                return 0;
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
