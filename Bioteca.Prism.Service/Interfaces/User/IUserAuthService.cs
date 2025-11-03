using Bioteca.Prism.Domain.Payloads.User;
using Bioteca.Prism.Domain.Responses;

namespace Bioteca.Prism.Service.Interfaces.User
{
    public interface IUserAuthService
    {
        public Task<UserLoginResponse> LoginAsync(UserLoginPayload payload);
        public Task<UserLoginResponse> RefreshTokenAsync(Guid researchId);
        public Task<string> EncryptAsync(string password);
    }
}

