namespace Bioteca.Prism.Domain.Entities.User
{
    public class User
    {
        public Guid Id { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Researcher.Researcher? Researcher { get; set; } 
    }
}
