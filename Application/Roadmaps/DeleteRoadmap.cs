using MediatR;
using Persistence;

namespace Application.Roadmaps
{
    public class DeleteRoadmap
    {
        public class Command : IRequest<Unit>
        {
            public Guid Id { get; set; }
            public bool IsDeleted { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private readonly DataContext _context;

            public Handler(DataContext context)
            {
                _context = context;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                // Fetch the roadmap from the database using the ID
                var roadmap = await _context.Roadmap.FindAsync(request.Id);

                // Check if the roadmap exists
                if (roadmap == null)
                {
                    throw new Exception($"Roadmap with ID {request.Id} not found.");
                }

                // Set the 'IsDeleted' flag to true (soft delete)
                roadmap.IsDeleted = request.IsDeleted;

                // Save changes to the database
                await _context.SaveChangesAsync(cancellationToken);

                // Return Unit to indicate that the operation is complete
                return Unit.Value;
            }
        }
    }
}
