using Bioteca.Prism.Data.Repositories.Volunteer;

namespace Bioteca.Prism.Service.Services.Volunteer;

/// <summary>
/// Service implementation for volunteer operations
/// </summary>
public class VolunteerService : Service<Domain.Entities.Volunteer.Volunteer, Guid>, IVolunteerService
{
    private readonly IVolunteerRepository _volunteerRepository;

    public VolunteerService(IVolunteerRepository repository) : base(repository)
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
