using Application.Dto;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Roadmaps
{
    public class List
    {
        public class Query : IRequest<Result>
        {
            public Guid UserId { get; set; }
            public string SearchTerm { get; set; } 
            public string Filter { get; set; }
            public int Page { get; set; } 
            public int PageSize { get; set; } 
            public string SortBy { get; set; }
        }

        public class Result
        {
            public int TotalCount { get; set; } // Total count of roadmaps (before pagination)
            public List<RoadmapDto> Items { get; set; } // Paginated roadmaps
            public int DraftCount { get; set; }
            public int PublishCount { get; set; }
            public int NotStartedCount { get; set; }
            public int InProgressCount { get; set; }
            public int CompletedCount { get; set; }
            public int NearDueCount { get; set; } // Count of near due roadmaps
            public int OverdueCount { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                var currentDate = DateTime.UtcNow; // Use UTC or DateTime.Now based on your requirements


                // Start the query
                var query = _context.Roadmap
                    .Where(r => r.UserId == request.UserId && !r.IsDeleted)
                    .Include(r => r.Nodes.OrderBy(n => n.CreateAt)) // Sort top-level nodes by CreatedAt
                        .ThenInclude(n => n.Children.OrderBy(c => c.CreateAt)) // Sort second-level nodes by CreatedAt
                        .ThenInclude(c => c.Children.OrderBy(cc => cc.CreateAt)) // Sort third-level nodes by CreatedAt
                    .OrderByDescending(r => r.CreatedAt)
                    .AsQueryable();

                // Apply search term filter if provided
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var trimmedSearchTerm = request.SearchTerm.Trim();

                    query = query.Where(r =>
                        EF.Functions.Like(r.RoadmapName.ToLower(), $"%{trimmedSearchTerm.ToLower()}%"));
                }

                var draftCount = await query.CountAsync(r => !r.IsPublished, cancellationToken);
                var publishCount = await query.CountAsync(r => r.IsPublished, cancellationToken);
                var notStartedCount = await query.CountAsync(r => r.IsPublished && !r.IsCompleted && !r.Nodes.Any(n => n.IsCompleted), cancellationToken);
                var inProgressCount = await query.CountAsync(r => r.IsPublished && !r.IsCompleted && r.Nodes.Any(n => n.IsCompleted), cancellationToken);
                var completedCount = await query.CountAsync(r => r.IsCompleted, cancellationToken);
                var nearDueCount = await query.CountAsync(r =>
                    !r.IsCompleted &&
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
                    !r.IsCompleted &&
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
                    case "publish":
                        query = query.Where(r => r.IsPublished);
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
                            !r.IsCompleted &&
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
                            !r.IsCompleted &&
                            r.Nodes.Where(n => n.ParentId == null)
                                   .OrderByDescending(n => n.EndDate)
                                   .Select(n => n.EndDate)
                                   .FirstOrDefault() < currentDate);
                        break;
                    case "all":
                    default:
                        break;
                }

                var roadmaps = await query.ToListAsync(cancellationToken);

                // Map the fetched roadmaps to RoadmapDto
                var roadmapDtos = roadmaps.Select(r => new RoadmapDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    RoadmapName = r.RoadmapName,
                    IsPublished = r.IsPublished,
                    IsCompleted = r.IsCompleted,
                    CreatedAt = r.CreatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAt = r.UpdatedAt.ToString("dd-MM-yyyy"),
                    UpdatedAtRaw = r.UpdatedAt,
                    CreatedAtRaw = r.CreatedAt,
                    CompletionRate = CalculateCompletionRate(r),
                    StartDate = r.Nodes
                        .Where(n => n.ParentId == null) // Only consider milestones
                        .OrderBy(n => n.StartDate)
                        .Select(n => n.StartDate.HasValue ? n.StartDate.Value.AddDays(1).Date.ToString("dd-MM-yyyy") : null)
                        .FirstOrDefault(),

                    EndDate = r.Nodes
                        .Where(n => n.ParentId == null) // Only consider milestones
                        .OrderByDescending(n => n.EndDate)
                        .Select(n => n.EndDate.HasValue ? n.EndDate.Value.AddDays(1).Date.ToString("dd-MM-yyyy") : null)
                        .FirstOrDefault(),


                    Nodes = r.Nodes
                        .Where(n => n.ParentId == null) // Only include root nodes
                        .OrderBy(n => n.CreateAt) // Sort root nodes by CreatedAt
                        .Select(MapNodeToDto) // Map nodes recursively
                        .ToList()
                }).ToList();

                roadmapDtos = request.SortBy switch
                {
                    "updatedAtdesc" => roadmapDtos.OrderBy(r => r.UpdatedAtRaw).ToList(),
                    "createdAt" => roadmapDtos.OrderByDescending(r => r.CreatedAtRaw).ToList(),
                    "createdAtdesc" => roadmapDtos.OrderBy(r => r.CreatedAtRaw).ToList(),
                    "progress" => roadmapDtos.OrderBy(r => r.CompletionRate).ToList(),
                    "progressdesc" => roadmapDtos.OrderByDescending(r => r.CompletionRate).ToList(),
                    "name" => roadmapDtos.OrderBy(r => r.RoadmapName).ToList(),
                    "namedesc" => roadmapDtos.OrderByDescending(r => r.RoadmapName).ToList(),
                    "startdate" => roadmapDtos.OrderBy(r => r.Nodes
                        .Where(n => n.ParentId == null)
                        .OrderBy(n => n.StartDate)
                        .Select(n => n.StartDate)
                        .FirstOrDefault()).ToList(),
                    "startdatedesc" => roadmapDtos.OrderByDescending(r => r.Nodes
                        .Where(n => n.ParentId == null)
                        .OrderBy(n => n.StartDate)
                        .Select(n => n.StartDate)
                        .FirstOrDefault()).ToList(),
                    "enddate" => roadmapDtos.OrderBy(r => r.Nodes
                        .Where(n => n.ParentId == null)
                        .OrderByDescending(n => n.EndDate)
                        .Select(n => n.EndDate)
                        .FirstOrDefault()).ToList(),
                    "enddatedesc" => roadmapDtos.OrderByDescending(r => r.Nodes
                        .Where(n => n.ParentId == null)
                        .OrderByDescending(n => n.EndDate)
                        .Select(n => n.EndDate)
                        .FirstOrDefault()).ToList(),
                    _ => roadmapDtos.OrderByDescending(r => r.UpdatedAtRaw).ToList()
                };

                var paginatedItems = roadmapDtos
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();


                // Return the result with total count and paginated items
                return new Result
                {
                    TotalCount = roadmapDtos.Count,
                    Items = paginatedItems,
                    DraftCount = draftCount,
                    PublishCount = publishCount,
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
