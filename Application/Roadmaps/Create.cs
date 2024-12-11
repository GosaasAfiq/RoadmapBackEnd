using Application.Dto;
using Domain;
using MediatR;
using Persistence;

namespace Application.Roadmaps
{
    public class Create
    {
        public class Command : IRequest
        {
            public CreateRoadmapDto Roadmap { get; set; }
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

                if (request.Roadmap == null)
                {
                    throw new ArgumentNullException(nameof(request.Roadmap), "Roadmap cannot be null.");
                }

                if (request.Roadmap.Milestones == null)
                {
                    throw new ArgumentNullException(nameof(request.Roadmap.Milestones), "Milestones cannot be null.");
                }

                var currentTime = DateTime.UtcNow;
                // Create the Roadmap
                var roadmap = new Roadmap
                {
                    Id = Guid.NewGuid(),
                    RoadmapName = request.Roadmap.Name,
                    UserId = request.Roadmap.UserId,
                    IsPublished = false, // set accordingly
                    IsCompleted = false, // set accordingly
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime,
                    Nodes = new List<Node>()
                };

                // Map Milestones
                foreach (var milestoneDto in request.Roadmap.Milestones)
                {

                    if (milestoneDto == null)
                    {
                        continue; // Skip null milestone if any
                    }

                    var milestone = new Node
                    {
                        Id = Guid.NewGuid(),
                        Name = milestoneDto.Name,
                        Description = milestoneDto.Description,
                        StartDate = milestoneDto.StartDate.ToUniversalTime(),
                        EndDate = milestoneDto.EndDate.ToUniversalTime(),
                        ParentId = null,
                        RoadmapId = roadmap.Id,
                        IsCompleted = false,
                        CreateAt = currentTime,
                        UpdatedAt = currentTime,
                        Children = new List<Node>()
                    };

                    // Map Sections
                    foreach (var sectionDto in milestoneDto.Sections)
                    {
                        if (sectionDto == null)
                        {
                            continue; // Skip null section if any
                        }

                        var section = new Node
                        {
                            Id = Guid.NewGuid(),
                            Name = sectionDto.Name,
                            Description = sectionDto.Description,
                            StartDate = sectionDto.StartDate.ToUniversalTime(),
                            EndDate = sectionDto.EndDate.ToUniversalTime(),
                            ParentId = milestone.Id,
                            RoadmapId = roadmap.Id,
                            IsCompleted = false,
                            CreateAt = currentTime,
                            UpdatedAt = currentTime,
                            Children = new List<Node>()
                        };

                        // Map Subsections
                        foreach (var subSectionDto in sectionDto.SubSections)
                        {
                            if (subSectionDto == null)
                            {
                                continue; // Skip null sub-section if any
                            }

                            var subSection = new Node
                            {
                                Id = Guid.NewGuid(),
                                Name = subSectionDto.Name,
                                Description = subSectionDto.Description,
                                StartDate = subSectionDto.StartDate.ToUniversalTime(),
                                EndDate = subSectionDto.EndDate.ToUniversalTime(),
                                ParentId = section.Id,
                                RoadmapId = roadmap.Id,
                                CreateAt = currentTime,
                                UpdatedAt = currentTime,
                                IsCompleted = false,
                            };

                            section.Children.Add(subSection);
                        }

                        milestone.Children.Add(section);
                    }

                    roadmap.Nodes.Add(milestone);
                }

                // Add the new roadmap to the context and save changes
                _context.Roadmap.Add(roadmap);
                await _context.SaveChangesAsync();
            }
        }
    }
}
