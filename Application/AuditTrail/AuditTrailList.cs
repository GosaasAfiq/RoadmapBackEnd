using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.AuditTrails
{
    public class AuditTrailList
    {
        public class Query : IRequest<List<AuditTrail>>
        {
            public string SearchTerm { get; set; } // Optional search term
            public string UserFilter { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }

        public class Handler : IRequestHandler<Query, List<AuditTrail>>
        {
            private readonly DataContext _context;
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<List<AuditTrail>> Handle(Query request, CancellationToken cancellationToken)
            {
                _logger.LogInformation("Fetching all audit trails.");

                var query = _context.AuditTrail.AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(a =>
                        EF.Functions.Like(a.Action.ToLower(), $"%{request.SearchTerm.ToLower()}%") ||
                        EF.Functions.Like(a.User.Username.ToLower(), $"%{request.SearchTerm.ToLower()}%"));
                }

                if (!string.IsNullOrEmpty(request.UserFilter))
                {
                    query = query.Where(a => a.User.Username.ToLower() == request.UserFilter.ToLower());
                }

                // Filter by start and end date
                if (request.StartDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp.Date >= request.StartDate.Value.Date); // Ensure date-only comparison
                }
                if (request.EndDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp.Date <= request.EndDate.Value.Date); // Fix here
                }



                var auditTrails = await query
                    .Include(a => a.User) // Include related User entity
                    .OrderByDescending(a => a.Timestamp)
                    .ToListAsync(cancellationToken);

                if (auditTrails.Count == 0)
                {
                    _logger.LogWarning("No audit trails found in the database.");
                }
                else
                {
                    _logger.LogInformation("Successfully fetched {Count} audit trails.", auditTrails.Count);
                }

                return auditTrails;
            }
        }
    }
}