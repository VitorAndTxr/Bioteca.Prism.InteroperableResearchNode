
using Bioteca.Prism.Core.Enumerators;

namespace Bioteca.Prism.Core.Exceptions;

/// <summary>
/// Exception para representar um HTTP 400 - BAD REQUEST
/// </summary>
public class BadRequestException : BaseException
{
    /// <summary>
    /// Construtor
    /// </summary>
    public BadRequestException() : base(400) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    public BadRequestException(string message) : base(400, message) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    /// <param name="innerException">Exception com a causa da exception atual.</param>
    public BadRequestException(string message, Exception innerException) : base(400, message, innerException) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode">Codigo de erro a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    public BadRequestException(string errorCode, string message) : base(errorCode, 400, message) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    /// <param name="innerException">Exception com a causa da exception atual.</param>
    public BadRequestException(string errorCode, string message, Exception innerException) : base(errorCode, 400, message, innerException) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="error">Enumeration erro a ser utilizado na ocorrência da Exception.</param>
    public BadRequestException(Enumeration error) : base(400, error) { }
}
