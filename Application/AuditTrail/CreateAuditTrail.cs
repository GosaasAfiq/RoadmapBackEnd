using Domain;
using MediatR;
using Persistence;

namespace Application.AuditTrails
{
    public class CreateAuditTrail
    {
        public class Command : IRequest<Unit>
        {
            public Guid UserId { get; set; }
            public string Action { get; set; }
            public DateTime Timestamp { get; set; }
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
                var currentTime = DateTime.UtcNow;

                var auditTrail = new AuditTrail  // Fixed instantiation
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Action = request.Action,
                    Timestamp = currentTime,
                };

                _context.AuditTrail.Add(auditTrail);

                var success = await _context.SaveChangesAsync(cancellationToken) > 0;

                if (!success)
                    throw new Exception("Failed to create the audit trail");

                return Unit.Value;
            }
        }
    }
}
