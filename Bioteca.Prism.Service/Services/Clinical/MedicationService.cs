using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Domain.Entities.Clinical;
using Bioteca.Prism.Service.Interfaces.Clinical;

namespace Bioteca.Prism.Service.Services.Clinical;

/// <summary>
/// Service implementation for medication operations
/// </summary>
public class MedicationService : BaseService<Medication, string>, IMedicationService
{
    private readonly IBaseRepository<Medication, string> _medicationRepository;

    public MedicationService(IBaseRepository<Medication, string> repository) : base(repository)
    {
        _medicationRepository = repository;
    }

    public async Task<List<Medication>> GetActiveMedicationsAsync(CancellationToken cancellationToken = default)
    {
        var allMedications = await _medicationRepository.GetAllAsync(cancellationToken);
        return allMedications.Where(m => m.IsActive).ToList();
    }

    public async Task<List<Medication>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var allMedications = await _medicationRepository.GetAllAsync(cancellationToken);
        return allMedications
            .Where(m => m.MedicationName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       m.ActiveIngredient.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<Medication?> GetByAnvisaCodeAsync(string anvisaCode, CancellationToken cancellationToken = default)
    {
        var allMedications = await _medicationRepository.GetAllAsync(cancellationToken);
        return allMedications.FirstOrDefault(m => m.AnvisaCode == anvisaCode);
    }
}
