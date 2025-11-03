using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Volunteer;
using Bioteca.Prism.Service.Interfaces.Volunteer;

namespace Bioteca.Prism.Service.Services.Volunteer;

/// <summary>
/// Service implementation for volunteer operations
/// </summary>
public class VolunteerService : BaseService<Domain.Entities.Volunteer.Volunteer, Guid>, IVolunteerService
{
    private readonly IVolunteerRepository _volunteerRepository;

    public VolunteerService(IVolunteerRepository repository, IApiContext apiContext) : base(repository, apiContext)
    {
        _volunteerRepository = repository;
    }

    public async Task<List<Domain.Entities.Volunteer.Volunteer>> GetByNodeIdAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _volunteerRepository.GetByNodeIdAsync(nodeId, cancellationToken);
    }

    public async Task<Domain.Entities.Volunteer.Volunteer?> GetByVolunteerCodeAsync(string volunteerCode, CancellationToken cancellationToken = default)
    {
        return await _volunteerRepository.GetByVolunteerCodeAsync(volunteerCode, cancellationToken);
    }
}


