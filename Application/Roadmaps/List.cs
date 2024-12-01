using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.Roadmaps
{
    public class List
    {
        public class Query : IRequest<List<Roadmap>> { }

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

                var roadmaps = await _context.Roadmap.ToListAsync(cancellationToken);

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
