using Bioteca.Prism.Core.Interfaces;
using Bioteca.Prism.Core.Paging;
using Bioteca.Prism.Core.Security;


namespace Bioteca.Prism.Core.Context;

/// <summary>
/// Contexto de trabalho do serviço
/// </summary>
public class ApiContext : IApiContext
{


    /// <summary>
    /// Contexto de paginação (privado)
    /// </summary>
    private PagingContext pagingContext = null;

    /// <summary>
    /// Contexto de segurança (privado)
    /// </summary>
    private SecurityContext securityContext = null;

    /// <summary>
    /// HTTP status code específico a ser retornado ao fim da requisição. Opcional. 
    /// </summary>
    public int? HttpStatusCode { get; set; } = null;



    /// <summary>
    /// Contexto de paginação.
    /// </summary>
    public PagingContext PagingContext
    {
        get
        {
            if (pagingContext == null)
            {
                pagingContext = new PagingContext();
            }
            return pagingContext;
        }
    }


    /// <summary>
    /// Contexto de segurança.
    /// </summary>
    public SecurityContext SecurityContext
    {
        get
        {
            if (securityContext == null)
            {
                securityContext = new SecurityContext();
            }
            return securityContext;
        }
    }
}
