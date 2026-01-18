using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Domain.DTOs.User;
using Bioteca.Prism.Domain.Payloads.User;

namespace Bioteca.Prism.Service.Interfaces.User
{
    /// <summary>
    /// User service interface - extends base CRUD operations with custom user methods
    /// </summary>
    public interface IUserService : IServiceBase<Domain.Entities.User.User, Guid>
    {
        /// <summary>
        /// Add a new user with encrypted password
        /// </summary>
        public Task<Domain.Entities.User.User?> AddAsync(AddUserPayload payload);

        /// <summary>
        /// Get all users paginated
        /// </summary>
        Task<List<UserDTO>> GetAllUserPaginateAsync();

        /// <summary>
        /// Get user by ID with Researcher navigation property
        /// </summary>
        Task<UserDTO?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Update an existing user (without password)
        /// </summary>
        Task<UserDTO?> UpdateUserAsync(Guid id, UpdateUserPayload payload);
    }
}

