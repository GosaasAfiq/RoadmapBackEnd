using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.Roadmaps
{
    public class List
    {
        public class Query : IRequest<List<Roadmap>> 
        {
            public Guid UserId { get; set; }
            public string SearchTerm { get; set; } // Optional search term
            public string Filter { get; set; } = "all"; // Default filter is 'all'

        }

        public class Handler : IRequestHandler<Query, List<Roadmap>>
        {
            private readonly DataContext _context;
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<List<Roadmap>> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching all roadmaps.");

                var query = _context.Roadmap
                    .Where(r => r.UserId == request.UserId);

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

                return roadmaps;
            }
        }
    }
}
