namespace Core.Api.Extensions;

public static class MappingExtensions
{
    /// <summary>
    /// Updates an entity property by invoking the specified action if the nullable value type has a value.
    /// </summary>
    /// <typeparam name="T">The value type of the nullable parameter.</typeparam>
    /// <param name="value">The nullable value to check.</param>
    /// <param name="updateAction">The action to invoke with the value if it is not null.</param>
    /// <remarks>
    /// This extension method is suitable for nullable value types such as int?, float?, DateTime?, and nullable enums.
    /// </remarks>
    public static void UpdateIfNotNull<T>(this T? value, Action<T> updateAction) 
        where T : struct
    {
        if (value.HasValue)
        {
            updateAction(value.Value);
        }
    }

    /// <summary>
    /// Updates a string property by invoking the specified action if the string is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="updateAction">The action to invoke with the string if it is not null, empty, or whitespace.</param>
    public static void UpdateIfNotNull(this string? value, Action<string> updateAction)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            updateAction(value);
        }
    }
}
