namespace Bioteca.Prism.Core.Paging;

/// <summary>
/// Contexto de paginação
/// </summary>
public class PagingContext
{
    /// <summary>
    /// Detalhes de paginação enviadas na request
    /// </summary>
    public RequestPaging RequestPaging { get; } = new RequestPaging();

    /// <summary>
    /// Detalhes de paginação a serem enviados na resposta
    /// </summary>
    public ResponsePaging ResponsePaging { get; } = new ResponsePaging();

    /// <summary>
    /// Retorna se a request atual é paginada
    /// </summary>
    public bool IsPaginated
    {
        get
        {
            return this.RequestPaging.PageSize > 0 ||
                   this.ResponsePaging.PageSize > 0;
        }
    }
}
