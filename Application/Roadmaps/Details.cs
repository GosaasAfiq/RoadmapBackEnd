using Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.Roadmaps
{
    public class Details
    {
        public class Query : IRequest<Roadmap>
        {
            public Guid Id { get; set; }
        }

        public class Handler : IRequestHandler<Query, Roadmap>
        {
            private readonly DataContext _context;
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<Roadmap> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching details for roadmap with ID: {RoadmapId}", request.Id);

                var roadmap = await _context.Roadmap.FindAsync(request.Id);

                if (roadmap == null)
                {
                    _logger.LogWarning("Roadmap with ID: {RoadmapId} not found", request.Id);
                }
                else
                {
                    _logger.LogInformation("Successfully fetched roadmap with ID: {RoadmapId}", request.Id);
                }

                return roadmap;
            }
        }
    }
}
