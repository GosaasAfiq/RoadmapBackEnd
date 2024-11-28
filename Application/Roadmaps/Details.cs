using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Persistence;

namespace Application.Roadmaps
{
    public class Details
    {
        public class Query : IRequest<Roadmap> 
        {
            public Guid Id {get; set;}
        }

        public class Handler : IRequestHandler<Query, Roadmap>
        {
        private readonly DataContext _context;
            public Handler(DataContext context)
            {
            _context = context;
            }

            public async Task<Roadmap> Handle(Query request, CancellationToken cancellationToken)
            {
                return await _context.Roadmap.FindAsync(request.Id);
            }
        }
    }
}