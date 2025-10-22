using Bioteca.Prism.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Bioteca.Prism.Core.Security
{
    /// <summary>
    /// Utility class for JWT handling
    /// </summary>
    public class JwtUtil : IJwtUtil
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Construtor
        /// </summary>
        public JwtUtil(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Creates a JWT using the "system default" private key
        /// </summary>
        /// <param name="claims">The list of claims to be included in payload</param>
        /// <returns>A base64 encoded JWT</returns>
        public string CreateJwt(List<Claim> claims)
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.UtcNow;

            string privateKey = _configuration.GetValue<string>("Jwt:Signer:PrivateKey");
            if (string.IsNullOrEmpty(privateKey))
            {
                throw new Exception("Unable to create Jwt.");
            }

            privateKey = privateKey.Replace("\\n", "\n");

            //Fixed Jti to make the token unique:
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            //Verifica se ja possui a claim de data de criação
            if (claims.Where(t => t.Type.Equals(JwtRegisteredClaimNames.Iat)).FirstOrDefault() == null)
            {
                //Fixed Iat to set the creation date of token
                var iat = (int)issueTime.Subtract(utc0).TotalSeconds;
                claims.Add(new Claim(JwtRegisteredClaimNames.Iat, iat.ToString()));
            }

            //Verifica se ja possui a claim de expiração
            if (claims.Where(t => t.Type.Equals(JwtRegisteredClaimNames.Exp)).FirstOrDefault() == null)
            {
                //Se configurado uma expiração no appsetings, seta no token o exp
                string expirationInMinutes = _configuration.GetValue<string>("Jwt:Expiration:Minutes");

                if (!string.IsNullOrEmpty(expirationInMinutes))
                {
                    int expiration = int.Parse(expirationInMinutes);
                    if (expiration > 0)
                    {
                        var exp = (int)issueTime.AddMinutes(expiration).Subtract(utc0).TotalSeconds;
                        claims.Add(new Claim(JwtRegisteredClaimNames.Exp, exp.ToString()));
                    }
                }
            }

            //Verifica se ja possui a claim de Audience
            if (claims.Where(t => t.Type.Equals(JwtRegisteredClaimNames.Aud)).FirstOrDefault() == null)
            {
                //Se configurado um audience, seta no token o aud
                string audience = _configuration.GetValue<string>("Jwt:Audience:Value");

                if (!string.IsNullOrEmpty(audience))
                {
                    claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
                }
            }


            //Verifica se ja possui a claim de Issuer
            if (claims.Where(t => t.Type.Equals(JwtRegisteredClaimNames.Iss)).FirstOrDefault() == null)
            {
                //Se configurado um Issuer, seta no token o iss
                string issuer = _configuration.GetValue<string>("Jwt:Issuer:Value");

                if (!string.IsNullOrEmpty(issuer))
                {
                    claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
                }
            }

            return CreateJwt(claims, privateKey);
        }

        /// <summary>
        /// Creates a JWT
        /// </summary>
        /// <param name="claims">The list of claims to be included in payload</param>
        /// <param name="privateRsaKey">The private key to sign the JWT</param>
        /// <returns>A base64 encoded JWT</returns>
        public string CreateJwt(List<Claim> claims, string privateRsaKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateRsaKey);
                Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
                return Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256);
            }
        }

        /// <summary>
        /// Decodes a JWT checking for the signature using the "system default" public key to validate the signer
        /// </summary>
        /// <param name="jwt">The base64 encoded JWT</param>
        /// <returns>An string with the JSON representing the JWT content. Throws an exception if signture check fails.</returns>
        public string DecodeJwt(string jwt)
        {
            string publicKey = _configuration.GetValue<string>("Jwt:Signer:PublicKey");
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new Exception("Unable to decode Jwt.");
            }

            publicKey = publicKey.Replace("\\n", "\n");
            return DecodeJwt(jwt, publicKey);
        }

        /// <summary>
        /// Decodes a JWT checking for the signature using the public key of the signer
        /// </summary>
        /// <param name="jwt">The base64 encoded JWT</param>
        /// <param name="publicRsaKey">The public key to sign the JWT</param>
        /// <returns>An string with the JSON representing the JWT content. Throws an exception if signture check fails.</returns>
        public string DecodeJwt(string jwt, string publicRsaKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicRsaKey);
                return Jose.JWT.Decode(jwt, rsa, Jose.JwsAlgorithm.RS256);
            }
        }
    }
}
