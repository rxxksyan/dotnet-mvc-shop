using System.Text.Json;

namespace SmartphoneShop.Web.Extensions;

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    public static void SetDecimal(this ISession session, string key, decimal value)
    {
        session.SetString(key, value.ToString());
    }

    public static decimal GetDecimal(this ISession session, string key)
    {
        var value = session.GetString(key);
        return decimal.TryParse(value, out var result) ? result : 0;
    }
}