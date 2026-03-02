using Bioteca.Prism.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bioteca.Prism.Core.Security.Authorization
{
    /// <summary>
    /// Auhtorization action filter
    /// </summary>
    public class AuthorizeActionFilter : IAuthorizationFilter
    {
        /// <summary>
        /// The required roles/claims to run an action. When empty or null the authorization filter will only check for a valid JWT, ignoring the token payload.
        /// </summary>
        private readonly string _requiredAuthorization = null;
        private readonly IApiContext _apiContext;
        private readonly IJwtUtil _jwtUtil;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor
        /// </summary>
        public AuthorizeActionFilter(string requiredAuthorization,
                                        IApiContext apiContext,
                                     IJwtUtil jwtUtil,
                                     IConfiguration configuration)
        {
            _requiredAuthorization = requiredAuthorization;
            _apiContext = apiContext;
            _configuration = configuration;
            _jwtUtil = jwtUtil;
        }

        /// <summary>
        /// Authorization validation method
        /// </summary>
        /// <param name="context">The authorization filter context</param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            //Gets the JWT from the current request headers
            string jwtToken = GetJwtFromHeader(context);

            //The JWT payloaded will be decoded in JSON format
            string jsonPayload = "";

            //If JWT is invalid or non-present - user is unauthorized
            if (!JwtIsValid(jwtToken, out jsonPayload))
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }

            // Adiciona o token e as claims do token na ApiContext
            _apiContext.SecurityContext.JwtToken = jwtToken;

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jsonToken = (JwtSecurityToken)handler.ReadToken(jwtToken);

            _apiContext.SecurityContext.Claims = jsonToken.Claims;

            _apiContext.SecurityContext.User = DeserializeClaims(jsonToken.Claims);

            // Validação das claims recebidas
            bool validateCreationMinDate = false;
            bool validateExpiration = false;
            bool ValidateAudience = false;
            bool ValidateIssuer = false;
            bool ValidateSubject = false;

            bool.TryParse(_configuration.GetValue<string>("Jwt:Creation:Validate"), out validateCreationMinDate);
            bool.TryParse(_configuration.GetValue<string>("Jwt:Expiration:Validate"), out validateExpiration);
            bool.TryParse(_configuration.GetValue<string>("Jwt:Audience:Validate"), out ValidateAudience);
            bool.TryParse(_configuration.GetValue<string>("Jwt:Issuer:Validate"), out ValidateIssuer);
            bool.TryParse(_configuration.GetValue<string>("Jwt:Subject:Validate"), out ValidateSubject);


            // Valida Subject
            try
            {
                if (ValidateSubject &&
                    string.IsNullOrEmpty(_apiContext.SecurityContext.Claims.Where(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).FirstOrDefault().Value))
                {
                    context.HttpContext.Response.StatusCode = 401;
                    context.Result = new UnauthorizedActionResult();
                    return;
                }
            }
            catch
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }

            // Valida Audience
            try
            {
                if (ValidateAudience &&
                    !_apiContext.SecurityContext.Claims.Where(c => c.Type.Equals(JwtRegisteredClaimNames.Aud)).FirstOrDefault().Value.Equals(_configuration.GetValue<string>("Jwt:Audience:Value")))
                {
                    context.HttpContext.Response.StatusCode = 401;
                    context.Result = new UnauthorizedActionResult();
                    return;
                }
            }
            catch
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }

            // Valida ValidateIssuer
            try
            {
                if (ValidateIssuer &&
                    !_apiContext.SecurityContext.Claims.Where(c => c.Type.Equals(JwtRegisteredClaimNames.Iss)).FirstOrDefault().Value.Equals(_configuration.GetValue<string>("Jwt:Issuer:Value")))
                {
                    context.HttpContext.Response.StatusCode = 401;
                    context.Result = new UnauthorizedActionResult();
                    return;
                }
            }
            catch
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }

            // Valida ValidateCreationMinDate
            try
            {
                if (validateCreationMinDate)
                {
                    var customerErpId = _apiContext.SecurityContext.Claims.FirstOrDefault(c => c.Type.Equals("customerErpId"))?.Value;
                    if (!string.IsNullOrEmpty(customerErpId) && customerErpId != "0") // Valida se é conta de usuário
                    {
                        var iat = _apiContext.SecurityContext.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Iat))?.Value;
                        var iatDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(iat)).ToLocalTime();
                        CultureInfo culture = CultureInfo.CreateSpecificCulture("pt-BR");

                        var configDate = DateTime.Parse(_configuration.GetValue<string>("Jwt:Creation:MinDate"), culture, DateTimeStyles.AssumeLocal);

                        if (iat == null || iatDate < configDate)
                        {
                            context.HttpContext.Response.StatusCode = 401;
                            context.Result = new UnauthorizedActionResult();
                            return;
                        }
                    }
                }
            }
            catch
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }

            // Valida ValidateExpiration
            try
            {
                if (validateExpiration)
                {
                    var exp = _apiContext.SecurityContext.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Exp))?.Value;
                    if (exp == null || new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(exp)) < DateTime.UtcNow)
                    {
                        context.HttpContext.Response.StatusCode = 401;
                        context.Result = new UnauthorizedActionResult();
                        return;
                    }
                }
            }
            catch
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }

            //If there  is a non-null required authorization, it need to be contained in the JWT payload
            if (!string.IsNullOrEmpty(_requiredAuthorization) &&
                !JsonPayloadContainsRequiredAuthorization(jsonPayload, _requiredAuthorization))
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new UnauthorizedActionResult();
                return;
            }
        }


        /// <summary>
        /// Gets the JWT from the current request headers
        /// </summary>
        /// <param name="context">The authorization filter context</param>
        /// <returns>The JWT received in the request headers if exists. Null otherwise.</returns>
        private string GetJwtFromHeader(AuthorizationFilterContext context)
        {
            try
            {
                string s = context.HttpContext.Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(s) ||
                    !s.StartsWith("Bearer "))
                {
                    return null;
                }

                s = s.Replace("Bearer ", "");
                return s;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a JWT is valid (checks for the signature)
        /// </summary>
        /// <param name="jwt">The JWT</param>
        /// <param name="jsonPayload">Output string to receive the decoded payload from the JWT (output string will be in JSON format)</param>
        /// <returns>True if JWT is valid. False otherwise.s</returns>
        private bool JwtIsValid(string jwt, out string jsonPayload)
        {
            jsonPayload = null;

            if (string.IsNullOrEmpty(jwt))
            {
                return false;
            }

            try
            {
                jsonPayload = _jwtUtil.DecodeJwt(jwt);
                if (string.IsNullOrEmpty(jsonPayload))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a JWT contains a role or claim.
        /// </summary>
        /// <param name="jsonPayload">The JSON payload decoded from the JWT</param>
        /// <param name="reqAuthorization">The required roles/claims to run an action.</param>
        /// <returns>True if the JSON payload contains the required role/claim. False otherwise.</returns>
        private bool JsonPayloadContainsRequiredAuthorization(string jsonPayload, string reqAuthorization)
        {
            if (string.IsNullOrEmpty(jsonPayload))
            {
                return false;
            }

            try
            {
                // try to get the claim from jwt payload:
                var claimValue = JObject.Parse(jsonPayload).Property(reqAuthorization)?.Value?.ToString();

                // claim not found
                if (claimValue == null)
                {
                    return false;
                }

                //if the claim exists, it's enough - REVISIT for a better handling
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deserializa a account dos claims
        /// retornando um objeto complexo com os dados do usuario
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        private User DeserializeClaims(IEnumerable<Claim> claims)
        {
            var account = new User();

            var claimSub = claims.FirstOrDefault(c => c.Type.Equals("sub"));
            Guid.TryParse(claimSub.Value, out Guid userId);
            account.Id = userId;

            account.Login = claims.FirstOrDefault(c => c.Type.Equals("login")).Value;

            var researcherIdClaim = claims.FirstOrDefault(c => c.Type.Equals("researcherId"));
            if (researcherIdClaim != null && Guid.TryParse(researcherIdClaim.Value, out Guid researcherId))
            {
                account.ResearcherId = researcherId;
            }

            return account;
        }
    }

}
