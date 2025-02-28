﻿using Application.Dto;
using Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Roadmaps
{
    public class Create
    {
        public class Command : IRequest
        {
            public CreateRoadmapDto Roadmap { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Roadmap).NotNull().WithMessage("Roadmap cannot be null.")
                    .SetValidator(new CreateRoadmapDtoValidator());

            }
        }

        public class CreateRoadmapDtoValidator : AbstractValidator<CreateRoadmapDto>
        {
            public CreateRoadmapDtoValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Roadmap name cannot be empty.")
                    .MaximumLength(40).WithMessage("Roadmap name cannot exceed 40 characters.");

                RuleFor(x => x.UserId)
                    .NotEmpty().WithMessage("UserId cannot be null or empty.");

                RuleFor(x => x.Milestones)
                   .NotEmpty().WithMessage("At least one milestone is required.")
                   .Must(HaveAtLeastOneSection).WithMessage("At least one section is required for the roadmap.")
                   .When(x => x.IsPublished);

                RuleForEach(x => x.Milestones)
                    .SetValidator(new MilestoneDtoValidatorP())
                    .When(x => x.IsPublished);

                RuleForEach(x => x.Milestones)
                    .SetValidator(new MilestoneDtoValidator())
                    .When(x => !x.IsPublished);
            }

            private bool HaveAtLeastOneSection(List<MilestoneDto> milestones)
            {
                return milestones != null && milestones.Any(m => m.Sections != null && m.Sections.Any());
            }
        }

        public class MilestoneDtoValidatorP : AbstractValidator<MilestoneDto>
        {
            public MilestoneDtoValidatorP()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Milestone name cannot be empty.")
                    .MaximumLength(40).WithMessage("Milestone name cannot exceed 40 characters.")
                    .When(x => x != null);

                RuleFor(x => x.StartDate)
                    .NotEmpty().WithMessage("Start date cannot be empty.")
                    .When(x => x != null);

                RuleFor(x => x.EndDate)
                    .NotEmpty().WithMessage("End date cannot be empty.")
                    .When(x => x != null);

                RuleFor(x => x.Description)
                    .NotEmpty().WithMessage("Description cannot be empty.")
                    .MaximumLength(400).WithMessage("Description cannot exceed 400 characters.")
                    .When(x => x != null);

                RuleForEach(x => x.Sections)
                    .SetValidator(new SectionDtoValidatorP());
            }
        }

        public class MilestoneDtoValidator : AbstractValidator<MilestoneDto>
        {
            public MilestoneDtoValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Milestone name cannot be empty.")
                    .MaximumLength(40).WithMessage("Milestone name cannot exceed 40 characters.")
                    .When(x => x != null);

                RuleForEach(x => x.Sections)
                    .SetValidator(new SectionDtoValidator());
            }
        }


        public class SectionDtoValidatorP : AbstractValidator<SectionDto>
        {
            public SectionDtoValidatorP()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Section name cannot be empty.")
                    .MaximumLength(40).WithMessage("Section name cannot exceed 40 characters.")
                    .When(x => x != null);

                RuleFor(x => x.StartDate)
                    .NotEmpty().WithMessage("Start date cannot be empty.")
                    .When(x => x != null);

                RuleFor(x => x.EndDate)
                    .NotEmpty().WithMessage("End date cannot be empty.")
                    .When(x => x != null);

                RuleFor(x => x.Description)
                    .NotEmpty().WithMessage("Description cannot be empty.")
                    .MaximumLength(400).WithMessage("Description cannot exceed 400 characters.")
                    .When(x => x != null);

                RuleForEach(x => x.SubSections)
                    .SetValidator(new SubSectionDtoValidatorP());
            }
        }

        public class SectionDtoValidator : AbstractValidator<SectionDto>
        {
            public SectionDtoValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Section name cannot be empty.")
                    .MaximumLength(40).WithMessage("Section name cannot exceed 40 characters.")
                    .When(x => x != null);

                RuleForEach(x => x.SubSections)
                    .SetValidator(new SubSectionDtoValidator());
            }
        }


        public class SubSectionDtoValidatorP : AbstractValidator<SubSectionDto>
        {
            public SubSectionDtoValidatorP()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("SubSection name cannot be empty.")
                    .MaximumLength(40).WithMessage("Subsection name cannot exceed 40 characters.")
                    .When(x => x != null);

                RuleFor(x => x.StartDate)
                    .NotEmpty().WithMessage("Start date cannot be empty.")
                    .When(x => x != null);

                RuleFor(x => x.EndDate)
                    .NotEmpty().WithMessage("End date cannot be empty.")
                    .When(x => x != null);

                RuleFor(x => x.Description)
                    .NotEmpty().WithMessage("Description cannot be empty.")
                    .MaximumLength(400).WithMessage("Description cannot exceed 400 characters.")
                    .When(x => x != null);
            }
        }

        public class SubSectionDtoValidator : AbstractValidator<SubSectionDto>
        {
            public SubSectionDtoValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("SubSection name cannot be empty.")
                    .MaximumLength(40).WithMessage("Subsection name cannot exceed 40 characters.")
                    .When(x => x != null);
            }
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

                var existingRoadmap = await _context.Roadmap
                    .FirstOrDefaultAsync(r => r.RoadmapName == request.Roadmap.Name && r.UserId == request.Roadmap.UserId && !r.IsDeleted, cancellationToken);

                if (existingRoadmap != null)
                {
                    throw new InvalidOperationException($"A roadmap with the name '{request.Roadmap.Name}' already exists."); 
                }


                // Create the Roadmap
                var roadmap = new Roadmap
                {
                    Id = Guid.NewGuid(),
                    RoadmapName = request.Roadmap.Name,
                    UserId = request.Roadmap.UserId,
                    IsPublished = request.Roadmap.IsPublished, // set accordingly
                    IsCompleted = false, // set accordingly
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp,
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
                        Description = milestoneDto.Description ?? string.Empty,
                        StartDate = milestoneDto.StartDate?.ToUniversalTime(),
                        EndDate = milestoneDto.EndDate?.ToUniversalTime(),
                        ParentId = null,
                        RoadmapId = roadmap.Id,
                        IsCompleted = false,
                        CreateAt = timestamp,
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

                        var section = new Node
                        {
                            Id = Guid.NewGuid(),
                            Name = sectionDto.Name,
                            Description = sectionDto.Description ?? string.Empty,  // Default to empty string if null
                            StartDate = sectionDto.StartDate?.ToUniversalTime(),
                            EndDate = sectionDto.EndDate?.ToUniversalTime(),
                            ParentId = milestone.Id,
                            RoadmapId = roadmap.Id,
                            IsCompleted = false,
                            CreateAt = timestamp,
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

                            var subSection = new Node
                            {
                                Id = Guid.NewGuid(),
                                Name = subSectionDto.Name,
                                Description = subSectionDto.Description ?? string.Empty,  // Default to empty string if null
                                StartDate = subSectionDto.StartDate?.ToUniversalTime(),
                                EndDate = subSectionDto.EndDate?.ToUniversalTime(),
                                ParentId = section.Id,
                                RoadmapId = roadmap.Id,
                                CreateAt = timestamp,
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

                // Add the new roadmap to the context and save changes
                _context.Roadmap.Add(roadmap);
                await _context.SaveChangesAsync();
            }
        }
    }
}
