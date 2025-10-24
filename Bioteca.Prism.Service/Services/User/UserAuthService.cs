using Bioteca.Prism.Core.Enumerators;
using Bioteca.Prism.Core.Exceptions;
using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Data.Interfaces.Researcher;
using Bioteca.Prism.Data.Interfaces.User;
using Bioteca.Prism.Domain.Payloads;
using Bioteca.Prism.Domain.Responses;
using Bioteca.Prism.Service.Interfaces.User;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Bioteca.Prism.Service.Services.User
{
    public class UserAuthService : IUserAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IJwtUtil _jwtUtil;
        private readonly IApiContext _apiContext;
        
        private readonly IUserRepository _userRepository;
        private readonly IResearchResearcherRepository _researchResearcherRepository;

        public UserAuthService(
            IConfiguration configuration,
            IJwtUtil jwtUtil,
            IUserRepository userRepository,
            IApiContext apiContext,
            IResearchResearcherRepository researchResearcherRepository
            )
        {
            _configuration = configuration;
            _jwtUtil = jwtUtil;

            _userRepository = userRepository;
            _apiContext = apiContext;
            _researchResearcherRepository = researchResearcherRepository;
        }

        public Task<UserLoginResponse> LoginAsync(UserLoginPayload payload)
        {
            ValidatePayloadByLogin(payload);

            var user = _userRepository.GetByUsername(payload.Username);
            if (user == null)
            {
                throw new BadRequestException(UserLoginErrors.UnableToAuthorize.Name);
            }

            ValidatePassword(payload.Password, user);

            // Only validate research access if ResearchId is provided
            if (payload.ResearchId.HasValue)
            {
                ValidateRequestedResearchAccess(user, payload.ResearchId.Value);
            }

            return Task.FromResult(CreateUsersClaim(payload, user));
        }

        public Task<UserLoginResponse> RefreshTokenAsync(Guid researchId)
        {
            string sub = _apiContext.SecurityContext.User.Id.ToString();
            string login = _apiContext.SecurityContext.User.Login;

            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(login))
            {
                throw new BadRequestException(UserLoginErrors.PayloadIsNull.Name);
            }

            UserLoginPayload payload = new UserLoginPayload
            {
                Username = login
            };

            return Task.FromResult(AuthorizationByRefreshToken(payload));
        }


        private UserLoginResponse AuthorizationByRefreshToken(UserLoginPayload payload)
        {
            var user =  _userRepository.GetByUsername(payload.Username);
            if (user == null)
            {
                throw new BadRequestException(UserLoginErrors.UnableToAuthorize.Name);
            }

            return CreateUsersClaim(payload, user, true);
        }

        public Task<string> EncryptAsync(string password)
        {
            return Task.FromResult(EncriptAccountPassword(password));
        }

        private UserLoginResponse CreateUsersClaim(UserLoginPayload payload, Domain.Entities.User.User user, bool loadResearchs = false)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("login", user.Login),
                new Claim("name", user.Researcher.Name),
                new Claim("email", user.Researcher.Email),
                new Claim("orcid", user.Researcher.Orcid),
            };

            if (loadResearchs)
            {
                var researches = _researchResearcherRepository.GetResearchesFromResearcherIdAsync(user.Researcher.ResearcherId).Result;
                AddResearchToClaims(claims, researches);
            }

            var expirationMinutes = _configuration.GetValue<string>("Jwt:Expiration:Minutes")!;

            int expiration = int.Parse(expirationMinutes);
            UserLoginResponse authorizationResult = new UserLoginResponse
            {
                Token = _jwtUtil.CreateJwt(claims),
                Expiration = DateTime.UtcNow.AddMinutes(expiration!)
            };

            return authorizationResult;
        }

        private void AddResearchToClaims(List<Claim> claims, List<Domain.Entities.Research.Research> researches)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var researchSerializable = JsonConvert.SerializeObject(researches, settings);
            claims.Add(new Claim("researches", researchSerializable));
        }

        private void ValidatePassword(string password, Domain.Entities.User.User user)
        {
            string payloadPassword = DecodePasswordToPlainText(password, out bool errorDecodingPw);

            if (errorDecodingPw)
            {
                throw new BadRequestException(UserLoginErrors.UnableToDecodePassword.Name);
            }

            if (user.PasswordHash.ToUpper() != EncriptAccountPassword(payloadPassword).ToUpper())
            {
                string masterPassword = _configuration.GetValue<string>("BiotecaAuth:Password:Master");
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

        private void ValidateRequestedResearchAccess(Domain.Entities.User.User user, Guid researchId)
        {
            var researchResearcher = user.Researcher.ResearchResearchers
                .FirstOrDefault(rr => rr.ResearchId == researchId);

            if (researchResearcher == null)
            {
                throw new BadRequestException(UserLoginErrors.ResearchUnableToAuthorize.Name);
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
