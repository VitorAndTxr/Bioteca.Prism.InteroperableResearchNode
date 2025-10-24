namespace Bioteca.Prism.Core.Paging;

/// <summary>
/// Detalhes de paginação a serem enviados na resposta
/// </summary>
public class ResponsePaging
{
    /// <summary>
    /// Indice do registro retornado
    /// </summary>
    public int CurrentRecord { get; set; }

    /// <summary>
    /// Tamanho de página do retorno
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total de registros disponíveis para serem retornadas
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Construtor
    /// </summary>
    public ResponsePaging()
    {
        this.CurrentRecord = 0;
        this.PageSize = 0;
        this.TotalRecords = 0;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="currentRecord">Indice do registro retornado</param>
    /// <param name="pageSize">Tamanho de página do retorno</param>
    /// <param name="totalRecords">Total de registros disponíveis para serem retornadas</param>
    public ResponsePaging(int currentRecord, int pageSize, int totalRecords)
    {
        this.SetValues(currentRecord, pageSize, totalRecords);
    }

    /// <summary>
    /// Define todos os valores dessa instancia do objeto ResponsePaging
    /// </summary>
    /// <param name="currentRecord">Indice do registro retornado</param>
    /// <param name="pageSize">Tamanho de página do retorno</param>
    /// <param name="totalRecords">Total de registros disponíveis para serem retornadas</param>
    public void SetValues(int currentRecord, int pageSize, int totalRecords)
    {
        this.CurrentRecord = currentRecord;
        this.PageSize = pageSize;
        this.TotalRecords = totalRecords;
    }
}
