using System.Text;

namespace Docfx.Plugins.DocDB;

internal static class OutputHelper
{
    public static string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> span = input.AsSpan();
        var builder = new ValueStringBuilder(stackalloc char[255]);

        bool wasPreviousUpper = false;
        bool isCurrentUpper;

        for (int i = 0; i < span.Length; i++)
        {
            char currentChar = span[i];
            isCurrentUpper = char.IsUpper(currentChar);

            if (isCurrentUpper)
            {
                // Add a space before this uppercase letter, unless it's the first character
                // or the previous character was also uppercase but followed by another uppercase letter.
                if (i > 0 && (!wasPreviousUpper || (i + 1 < span.Length && char.IsLower(span[i + 1]))))
                {
                    builder.Append(' ');
                }

                wasPreviousUpper = true;
            }
            else
            {
                wasPreviousUpper = false;
            }

            // Append the current character (lowercased if it's an uppercase letter and not part of an acronym)
            builder.Append(isCurrentUpper && (!wasPreviousUpper || (i + 1 < span.Length && char.IsLower(span[i + 1])))
                ? char.ToLowerInvariant(currentChar)
                : currentChar);
        }

        return builder.ToString();
    }
}
