using System.ComponentModel;
using System.Reflection;

namespace BL.Utils;

public static class EnumExtensions
{
    public static string GetDescription<T>(this T enumValue) where T : Enum
    {
        var result = enumValue.ToString();
        var field = enumValue.GetType().GetField(result);

        if (field == null)
            return result;

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? result;
    }

    public static Dictionary<string, T> GetDescriptionMapping<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .ToDictionary(GetDescription, e => e);
    }
}