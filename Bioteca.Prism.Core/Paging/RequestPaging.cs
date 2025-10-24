namespace Bioteca.Prism.Core.Paging;

/// <summary>
/// Detalhes de paginação enviados na requisição
/// </summary>
public class RequestPaging
{
    /// <summary>
    /// Índice da página a ser retornada
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Quantidade de registros por página a serem retornados
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Construtor
    /// </summary>
    public RequestPaging()
    {
        this.Page = 0;
        this.PageSize = 0;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="page">Índice da página a ser retornada</param>
    /// <param name="pageSize">Quantidade de registros por página a serem retornados</param>
    public RequestPaging(int page, int pageSize)
    {
        this.Page = page;
        this.PageSize = pageSize;
    }
}
