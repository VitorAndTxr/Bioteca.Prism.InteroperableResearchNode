namespace Bioteca.Prism.Domain.Responses
{
    public class UserLoginResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
}
