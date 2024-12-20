using Application.Dto;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Roadmaps
{
    public class UpdateRoadmap
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

                var currentTime = DateTime.UtcNow;
                var timestamp = currentTime;

                var oldRoadmap = await _context.Roadmap
                    .Include(r => r.Nodes)
                    .FirstOrDefaultAsync(r => r.Id == request.Roadmap.Id, cancellationToken);

                // Create the Roadmap
                var roadmap = new Roadmap
                {
                    Id = oldRoadmap.Id,
                    RoadmapName = request.Roadmap.Name,
                    UserId = request.Roadmap.UserId,
                    IsPublished = request.Roadmap.IsPublished, // set accordingly
                    IsCompleted = false, // set accordingly
                    CreatedAt = oldRoadmap.CreatedAt,
                    UpdatedAt = timestamp,
                    Nodes = new List<Node>()
                };

                Node FindMatchingOldNode(Guid nodeId)
                {
                    return oldRoadmap.Nodes.FirstOrDefault(oldNode => oldNode.Id == nodeId);
                }

                // Map Milestones
                foreach (var milestoneDto in request.Roadmap.Milestones)
                {

                    if (milestoneDto == null)
                    {
                        continue; // Skip null milestone if any
                    }

                    var milestoneId = string.IsNullOrEmpty(milestoneDto.Id?.ToString())
                                    ? Guid.NewGuid()
                                    : Guid.Parse(milestoneDto.Id.ToString());

                    var oldMilestone = FindMatchingOldNode(milestoneId);

                    var milestone = new Node
                    {
                        Id = milestoneId,
                        Name = milestoneDto.Name,
                        Description = milestoneDto.Description ?? string.Empty,
                        StartDate = milestoneDto.StartDate?.ToUniversalTime(),
                        EndDate = milestoneDto.EndDate?.ToUniversalTime(),
                        ParentId = null,
                        RoadmapId = roadmap.Id,
                        IsCompleted = false,
                        CreateAt = oldMilestone?.CreateAt ?? timestamp,
                        UpdatedAt = timestamp,
                        Children = new List<Node>()
                    };

                    timestamp = timestamp.AddMilliseconds(10);

                    // Map Sections
                    foreach (var sectionDto in milestoneDto.Sections)
                    {
                        if (sectionDto == null)
                        {
                            continue; // Skip null section if any
                        }

                        var sectionId = string.IsNullOrEmpty(sectionDto.Id?.ToString())
                           ? Guid.NewGuid()
                           : Guid.Parse(sectionDto.Id.ToString());

                        var oldSection = FindMatchingOldNode(sectionId);

                        var section = new Node
                        {
                            Id = sectionId,
                            Name = sectionDto.Name,
                            Description = sectionDto.Description ?? string.Empty,  // Default to empty string if null
                            StartDate = sectionDto.StartDate?.ToUniversalTime(),
                            EndDate = sectionDto.EndDate?.ToUniversalTime(),
                            ParentId = milestone.Id,
                            RoadmapId = roadmap.Id,
                            IsCompleted = false,
                            CreateAt = oldSection?.CreateAt ?? timestamp,
                            UpdatedAt = timestamp,
                            Children = new List<Node>()
                        };

                        timestamp = timestamp.AddMilliseconds(10);

                        // Map Subsections
                        foreach (var subSectionDto in sectionDto.SubSections)
                        {
                            if (subSectionDto == null)
                            {
                                continue; // Skip null sub-section if any
                            }

                            var subSectionId = string.IsNullOrEmpty(subSectionDto.Id?.ToString())
                               ? Guid.NewGuid()
                               : Guid.Parse(subSectionDto.Id.ToString());

                            var oldSubSection = FindMatchingOldNode(subSectionId);

                            var subSection = new Node
                            {
                                Id = subSectionId,
                                Name = subSectionDto.Name,
                                Description = subSectionDto.Description ?? string.Empty,  // Default to empty string if null
                                StartDate = subSectionDto.StartDate?.ToUniversalTime(),
                                EndDate = subSectionDto.EndDate?.ToUniversalTime(),
                                ParentId = section.Id,
                                RoadmapId = roadmap.Id,
                                CreateAt = oldSubSection?.CreateAt ?? timestamp,
                                UpdatedAt = timestamp,
                                IsCompleted = false,
                            };

                            timestamp = timestamp.AddMilliseconds(10);

                            section.Children.Add(subSection);
                        }

                        milestone.Children.Add(section);
                    }

                    roadmap.Nodes.Add(milestone);
                }

                Console.WriteLine("Roadmap" + roadmap);

                _context.Roadmap.Remove(oldRoadmap);

                // Add the new roadmap to the context and save changes
                _context.Roadmap.Add(roadmap);
                await _context.SaveChangesAsync();
            }
        }
    }
}
