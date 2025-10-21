
using Bioteca.Prism.Core.Enumerators;

namespace Bioteca.Prism.Core.Exceptions;

/// <summary>
/// Exception base na construção dos serviços.
/// </summary>
public class BaseException : Exception
{
    /// <summary>
    /// Referencia interna o HTTP status code a ser utilizado na ocorrência da Exception.
    /// </summary>
    private int? _httpStatusCode = null;

    /// <summary>
    /// Referencia interna ao codigo de erro a ser utilizado na ocorrência da Exception.
    /// </summary>
    private string _errorCode = null;

    /// <summary>
    /// HTTP status code a ser utilizado na ocorrência da Exception.
    /// </summary>
    public virtual int? HttpStatusCode
    {
        get
        {
            return _httpStatusCode;
        }
    }

    /// <summary>
    /// Codigo de erro a ser utilizado na ocorrência da Exception.
    /// </summary>
    public string ErrorCode
    {
        get
        {
            return _errorCode;
        }
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    public BaseException(int httpStatusCode) : base()
    {
        _httpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    public BaseException(int httpStatusCode, string message) : base(message)
    {
        _httpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    /// <param name="innerException">Exception com a causa da exception atual.</param>
    public BaseException(int httpStatusCode, string message, Exception innerException) : base(message, innerException)
    {
        _httpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode">Codigo de erro a ser utilizado na ocorrência da Exception.</param>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    public BaseException(string errorCode, int httpStatusCode) : base()
    {
        _errorCode = errorCode;
        _httpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode">Codigo de erro a ser utilizado na ocorrência da Exception.</param>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    public BaseException(string errorCode, int httpStatusCode, string message) : base(message)
    {
        _errorCode = errorCode;
        _httpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode">Codigo de erro a ser utilizado na ocorrência da Exception.</param>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    /// <param name="innerException">Exception com a causa da exception atual.</param>
    public BaseException(string errorCode, int httpStatusCode, string message, Exception innerException) : base(message, innerException)
    {
        _errorCode = errorCode;
        _httpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="httpStatusCode">HTTP status code a ser utilizado na ocorrência da Exception.</param>
    /// <param name="error">Enumeration erro a ser utilizado na ocorrência da Exception.</param>
    public BaseException(int httpStatusCode, Enumeration error) : base(error.Name)
    {
        _errorCode = error.Code;
        _httpStatusCode = httpStatusCode;
    }


    /// <summary>
    /// Construtor
    /// </summary>
    public BaseException() : base() { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    public BaseException(string message) : base(message) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    /// <param name="innerException">Exception com a causa da exception atual.</param>
    public BaseException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode">Codigo de erro a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    public BaseException(string errorCode, string message) : base(message) { }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="errorCode">Codigo de erro a ser utilizado na ocorrência da Exception.</param>
    /// <param name="message">Mensagem descrevendo a exception.</param>
    /// <param name="innerException">Exception com a causa da exception atual.</param>
    public BaseException(string errorCode, string message, Exception innerException) : base(message, innerException) { }
}
