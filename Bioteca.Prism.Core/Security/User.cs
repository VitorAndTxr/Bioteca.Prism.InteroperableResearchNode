namespace Bioteca.Prism.Core.Security;

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public Guid? ResearcherId { get; set; }
}

