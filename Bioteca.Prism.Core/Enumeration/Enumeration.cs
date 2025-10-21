using System.Reflection;

namespace Bioteca.Prism.Core.Enumerators;

/// <summary>
/// Enumeration
/// </summary>
public abstract class Enumeration : IComparable
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Code
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Id
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Construtor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="code"></param>
    /// <param name="name"></param>
    protected Enumeration(int id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    /// <summary>
    /// Retorna o Name da Enumeration
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;

    /// <summary>
    /// Retorna todos os valores da Enumeration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<T> GetAll<T>() where T : Enumeration
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)).Cast<T>();
    }

    /// <summary>
    /// Comparador
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        var otherValue = obj as Enumeration;

        if (otherValue == null)
            return false;

        var typeMatches = GetType().Equals(obj.GetType());
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    /// <summary>
    /// Retorna um hashcode do Id
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Comparador absoluto
    /// </summary>
    /// <param name="firstValue"></param>
    /// <param name="secondValue"></param>
    /// <returns></returns>
    public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
    {
        var absoluteDifference = Math.Abs(firstValue.Id - secondValue.Id);
        return absoluteDifference;
    }

    /// <summary>
    /// Busca o Enumeration pelo Id
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T FromValue<T>(int value) where T : Enumeration
    {
        var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
        return matchingItem;
    }

    /// <summary>
    /// Busca o Enumeration pelo Name
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="displayName"></param>
    /// <returns></returns>
    public static T FromDisplayName<T>(string displayName) where T : Enumeration
    {
        var matchingItem = Parse<T, string>(displayName, "display name", item => item.Name == displayName);
        return matchingItem;
    }

    /// <summary>
    /// Busca o Enumeration pelo Code
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="code"></param>
    /// <returns></returns>
    public static T FromCode<T>(string code) where T : Enumeration
    {
        var matchingItem = Parse<T, string>(code, "code", item => item.Code == code);
        return matchingItem;
    }

    private static T Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem == null)
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");

        return matchingItem;
    }

    /// <summary>
    /// Comparador pelo Id
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);
}
