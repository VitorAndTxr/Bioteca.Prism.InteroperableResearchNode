using Bioteca.Prism.Core.Enumerators;
using Bioteca.Prism.Core.Exceptions;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Domain.Payloads;
using Bioteca.Prism.Domain.Responses;
using Bioteca.Prism.Service.Interfaces.User;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Bioteca.Prism.Service.Services.User
{
    public class UserAuthService : IUserAuthService
    {
        private readonly IConfiguration _configuration;
        
        private readonly IUserRepository _userRepository;
        public UserAuthService(
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<UserLoginResponse> LoginAsync(UserLoginPayload payload)
        {
            ValidatePayloadByLogin(payload);

            var account = _accountRepository.GetByLogin(payload.Login).Result;
            if (account == null)
            {
                throw new BadRequestException(UserLoginErrors.UnableToAuthorize.Name);
            }

            ValidatePassword(payload.Password, account);

            return CreateUsersClaim(payload, account);
        }

        public Task<UserLoginResponse> RefreshTokenAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> EncryptAsync(string password)
        {
            return Task.FromResult(EncriptAccountPassword(password));
        }

        private void ValidatePassword(string password, Account account)
        {
            string payloadPassword = DecodePasswordToPlainText(password, out bool errorDecodingPw);

            if (errorDecodingPw)
            {
                throw new BadRequestException(UserLoginErrors.UnableToDecodePassword.Name);
            }

            if (account.Password.ToUpper() != EncriptAccountPassword(payloadPassword).ToUpper())
            {
                string masterPassword = _configuration.GetValue<string>("AccountService:Password:Master");
                if (masterPassword.ToUpper() != payloadPassword.ToUpper())
                {
                    throw new BadRequestException(UserLoginErrors.UnableToAuthorize.Name);
                }
            }
        }

        private void ValidatePayloadByLogin(UserLoginPayload payload)
        {
            if (payload == null)
            {
                throw new BadRequestException(UserLoginErrors.PayloadIsNull.Name);
            }

            if (string.IsNullOrEmpty(payload.Username))
            {
                throw new BadRequestException(UserLoginErrors.LoginNullOrEmpty.Name);
            }

            if (string.IsNullOrEmpty(payload.Password))
            {
                throw new BadRequestException(UserLoginErrors.PasswordNullOrEmpty.Name);
            }
        }

        private string EncriptAccountPassword(string password)
        {
            using var sha512 = SHA512.Create();
            var hashedBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            return hash;
        }

        private string DecodePasswordToPlainText(string password, out bool errorDecoding)
        {
            try
            {
                errorDecoding = false;
                string pwDecoded64 = Encoding.UTF8.GetString(Convert.FromBase64String(password));
                return pwDecoded64;
            }
            catch
            {
                errorDecoding = true;
                return null;
            }
        }
    }
}
