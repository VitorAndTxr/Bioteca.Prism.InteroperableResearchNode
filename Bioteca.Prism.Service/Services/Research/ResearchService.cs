using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Node;
using Bioteca.Prism.Data.Interfaces.Research;
using Bioteca.Prism.Domain.DTOs.Research;
using Bioteca.Prism.Domain.DTOs.ResearchNode;
using Bioteca.Prism.Service.Interfaces.Research;

namespace Bioteca.Prism.Service.Services.Research;

/// <summary>
/// Service implementation for research project operations
/// </summary>
public class ResearchService : BaseService<Domain.Entities.Research.Research, Guid>, IResearchService
{
    private readonly IResearchRepository _researchRepository;
    private readonly INodeRepository _nodeRepository;

    public ResearchService(IResearchRepository repository, INodeRepository nodeRepository, IApiContext apiContext) : base(repository, apiContext)
    {
        _researchRepository = repository;
        _nodeRepository = nodeRepository;
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetByStatusAsync(status, cancellationToken);
    }

    public async Task<List<Domain.Entities.Research.Research>> GetActiveResearchAsync(CancellationToken cancellationToken = default)
    {
        return await _researchRepository.GetActiveResearchAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Research.Research> AddAsync(AddResearchDTO payload)
    {
        ValidateAddResearchPayload(payload);

        var research = new Domain.Entities.Research.Research
        {
            Id = Guid.NewGuid(),
            Title = payload.Title,
            Description = payload.Description,
            ResearchNodeId = payload.ResearchNodeId,
            StartDate = DateTime.UtcNow,
            Status = "Planning",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _researchRepository.AddAsync(research);
    }

    public async Task<List<ResearchDTO>> GetAllPaginateAsync()
    {
        var result = await _researchRepository.GetPagedAsync();

        var mappedResult = result.Select(research => new ResearchDTO
        {
            Id = research.Id,
            Title = research.Title,
            Description = research.Description,
            EndDate = research.EndDate,
            Status = research.Status,
            ResearchNode = research.ResearchNode != null ? new ResearchNodeConnectionDTO
            {
                Id = research.ResearchNode.Id,
                NodeName = research.ResearchNode.NodeName,
                NodeUrl = research.ResearchNode.NodeUrl,
                Status = research.ResearchNode.Status,
                NodeAccessLevel = research.ResearchNode.NodeAccessLevel,
                RegisteredAt = research.ResearchNode.RegisteredAt,
                UpdatedAt = research.ResearchNode.UpdatedAt
            } : null!
        }).ToList();

        return mappedResult;
    }

    private void ValidateAddResearchPayload(AddResearchDTO payload)
    {
        if (string.IsNullOrEmpty(payload.Title))
        {
            throw new Exception("Research title is required");
        }

        if (string.IsNullOrEmpty(payload.Description))
        {
            throw new Exception("Research description is required");
        }

        if (payload.ResearchNodeId == Guid.Empty)
        {
            throw new Exception("Research node ID is required");
        }

        // Verify that the research node exists
        var node = _nodeRepository.GetByIdAsync(payload.ResearchNodeId).Result;
        if (node == null)
        {
            throw new Exception("Research node does not exist");
        }
    }
}
