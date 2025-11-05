using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Service;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Domain.DTOs.Researcher;
using Bioteca.Prism.Domain.DTOs.User;
using Bioteca.Prism.Domain.Entities.Researcher;
using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.Service.Interfaces.User;

namespace Bioteca.Prism.Service.Services.User;

public class UserService: BaseService<Domain.Entities.User.User, Guid>, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IResearcherRepository _researcherRepository;
    private readonly IUserAuthService _userAuthService;

    public UserService(
        IUserRepository repository,
        IResearcherRepository researcherRepository,
        IUserAuthService userAuthService,
        IApiContext apiContext
        ) : base(repository, apiContext)
    {
        _userRepository = repository;
        _researcherRepository = researcherRepository;
        _userAuthService = userAuthService;
    }

    public async Task<Domain.Entities.User.User?> AddAsync(AddUserPayload payload)
    {
        Domain.Entities.Researcher.Researcher? researcher;

        ValidateAddUserPayload(payload, out researcher);

        Domain.Entities.User.User user = new Domain.Entities.User.User
        {
            Id = Guid.NewGuid(),
            Login = payload.Login,
            PasswordHash = _userAuthService.EncryptAsync(payload.Password).Result,
            Role = payload.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Researcher = researcher != null ? researcher : null
        };

        return await _userRepository.AddAsync(user);
    }

    private void ValidateAddUserPayload(AddUserPayload payload, out Domain.Entities.Researcher.Researcher? researcher)
    {
        if (string.IsNullOrEmpty(payload.Login) || string.IsNullOrEmpty(payload.Password) || string.IsNullOrEmpty(payload.Role))
        {
            throw new Exception("Invalid payload");
        }

        if (_userRepository.GetByUsername(payload.Login) != null)
        {
            throw new Exception("User already exists");
        }
        researcher = null;
        if (payload.ResearcherId != null)
        {
            researcher = _researcherRepository.GetByIdAsync(payload.ResearcherId.Value).Result;

            if (researcher == null)
            {
                throw new Exception("Researcher does not exist");
            }
        }
    }

    public async Task<List<UserDTO>> GetAllUserPaginateAsync()
    {
        var result = await _userRepository.GetPagedAsync();

        var mappedResult = result.Select(user => new UserDTO
        {
            Id = user.Id,
            Login = user.Login,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Researcher = user.Researcher != null ? new ResearcherInfoDto()
            {
                Name = user.Researcher.Name,
                Email = user.Researcher.Email,
                Role = user.Researcher.Role,
                Orcid = user.Researcher.Orcid
            } : null
        }).ToList();

        return mappedResult;
    }
}


