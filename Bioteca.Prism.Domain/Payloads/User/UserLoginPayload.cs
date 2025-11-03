namespace Bioteca.Prism.Domain.Payloads.User
{
    public class UserLoginPayload
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Token { get; set; }

        public Guid? ResearchId { get; set; }
    }
}
