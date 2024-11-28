using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Roadmaps
{
    public class List
    {
        public class Query : IRequest<List<Roadmap>> {}

        public class Handler : IRequestHandler<Query, List<Roadmap>>
        {
        private readonly DataContext _context;
            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<List<Roadmap>> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.Roadmap.ToListAsync();
            }
        }
    }
}