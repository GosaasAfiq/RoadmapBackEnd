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

                if (request.Filter != "all")
                {
                    bool isPublished = request.Filter == "not-started"; // You can customize the logic based on the filter values
                    query = query.Where(r => r.IsPublished == isPublished);
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
                    RoadmapName = r.RoadmapName,
                    IsPublished = r.IsPublished,
                    Nodes = r.Nodes
                        .Where(n => n.ParentId == null) // Only include root nodes
                        .OrderBy(n => n.CreateAt) // Sort root nodes by CreatedAt
                        .Select(MapNodeToDto) // Map nodes recursively
                        .ToList()
                }).ToList();

                return roadmapDtos;
            }
            private NodeDto MapNodeToDto(Node node)
            {
                return new NodeDto
                {
                    Id = node.Id,
                    Name = node.Name,
                    Description = node.Description,
                    IsCompleted = node.IsCompleted,
                    StartDate = node.StartDate,
                    EndDate = node.EndDate,
                    Children = node.Children
                        .OrderBy(c => c.CreateAt)
                        .Select(MapNodeToDto) // Recursively map child nodes
                        .ToList()
                };
            }
        }
    }
}
