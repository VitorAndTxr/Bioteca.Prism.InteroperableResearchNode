namespace Bioteca.Prism.Domain.Payloads.User;

public class AddUserPayload { 

    public string Login { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }

    public Guid? ResearcherId { get; set; }
}


