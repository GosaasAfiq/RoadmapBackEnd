using Application.Dto;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Application.Roadmaps
{
    public class UserList
    {
        public class Query : IRequest<List<User>>{}

        public class Handler : IRequestHandler<Query, List<User>>
        {
            private readonly DataContext _context;
            private readonly ILogger<Handler> _logger;

            public Handler(DataContext context, ILogger<Handler> logger)
            {
                _context = context;
                _logger = logger;
            }

            public async Task<List<User>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.User.ToListAsync();
            }
        }
    }
}
