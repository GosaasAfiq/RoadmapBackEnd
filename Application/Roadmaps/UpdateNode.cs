using Domain;
using MediatR;
using Persistence;

namespace Application.Roadmaps
{
    public class UpdateNode
    {
        public class Command : IRequest
        {
            public Roadmap Roadmap { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly DataContext _context;
            public Handler(DataContext context)
            {
                _context = context;
            }


            public async Task Handle(Command request, CancellationToken cancellationToken)
            {
                Console.WriteLine("Roadmap first node " + request.Roadmap.Nodes.ElementAt(0).Id);
            }
        }
    }
}
