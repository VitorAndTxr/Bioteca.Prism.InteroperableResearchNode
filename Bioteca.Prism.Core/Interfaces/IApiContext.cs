using Bioteca.Prism.Core.Paging;
using Bioteca.Prism.Core.Security;

namespace  Bioteca.Prism.Core.Interfaces;

/// <summary>
/// Interface do Contexto de trabalho do serviço
/// </summary>
public interface IApiContext
{
    /// <summary>
    /// HTTP status code específico a ser retornado ao fim da requisição. Opcional. 
    /// </summary>
    int? HttpStatusCode { get; set; }

    /// <summary>
    /// Lista de erros a ser retornado ao fim da requisição. Opcional.
    /// </summary>
    //List<Error> Errors { get; }

    /// <summary>
    /// Contexto de paginação.
    /// </summary>
    PagingContext PagingContext { get; }


    /// <summary>
    /// Contexto de segurança
    /// </summary>
    SecurityContext SecurityContext { get; }
}


