﻿using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.AuditTrails
{
    public class AuditTrailList
    {
        public class Query : IRequest<Result>
        {
            public string SearchTerm { get; set; } // Optional search term
            public string UserFilter { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int Page { get; set; } = 1; // Default to page 1
            public int PageSize { get; set; } = 6; // Default to 6 items per page
        }

        public class Result
        {
            public int TotalCount { get; set; }
            public List<AuditTrail> Items { get; set; }
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

                var totalCount = await query.CountAsync(cancellationToken); // Get total count

                var items = await query
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    TotalCount = totalCount,
                    Items = items
                };
            }
        }
    }
}