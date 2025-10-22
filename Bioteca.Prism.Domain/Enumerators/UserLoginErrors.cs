
namespace Bioteca.Prism.Core.Enumerators;

public class UserLoginErrors : Enumeration
{
    public UserLoginErrors(int id, string code, string name) : base(id, code, name) { }

    public static UserLoginErrors PayloadIsNull = new UserLoginErrors(1, "UL001", "Payload não pode ser nulo.");
    public static UserLoginErrors LoginNullOrEmpty = new UserLoginErrors(2, "UL002", "Login nulo ou não enviado.");
    public static UserLoginErrors PasswordNullOrEmpty = new UserLoginErrors(3, "UL003", "Senha nula ou não enviada.");
    public static UserLoginErrors UnableToAuthorize = new UserLoginErrors(4, "UL004", "Não foi possível autorizar com as credenciais fornecidas.");
    public static UserLoginErrors UnableToDecodePassword = new UserLoginErrors(5, "UL005", "Não foi possível realizar o decode da senha.");
    public static UserLoginErrors WithoutPermissions = new UserLoginErrors(6, "UL006", "Usuário não possuí permissões para ULessar esse sistema.");
    public static UserLoginErrors GraphTokenEmpty = new UserLoginErrors(7, "UL007", "Microsoft Token esta ausente.");
    public static UserLoginErrors ULcountNotFound = new UserLoginErrors(8, "UL008", "Conta do cliente não encontrada.");
    public static UserLoginErrors ResearchUnableToAuthorize = new UserLoginErrors(9, "UL009", "Conta de usuário não autorizada para esta pesquisa.");
    public static UserLoginErrors AuthorizationNotImplemented = new UserLoginErrors(10, "UL0010", "Tipo de autenticação não implementada.");
    public static UserLoginErrors GoogleTokenEmpty = new UserLoginErrors(11, "UL011", "Google Token esta ausente.");
    public static UserLoginErrors GoogleUserDoestMULhToken = new UserLoginErrors(12, "UL012", "Email que solicitou login não bate com o token enviado.");
    public static UserLoginErrors UsersPerApplicationNull = new UserLoginErrors(13, "UL013", "Não foi possível retornar o total de usuarios para esta pesquisa.");
}
