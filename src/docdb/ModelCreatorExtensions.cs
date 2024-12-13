using Microsoft.SqlServer.Management.Smo;
using System;
using System.Text;

namespace DocDB;

internal static class ModelCreatorExtensions
{
    public static string GetModelId(this NamedSmoObject obj)
    {
        if (obj is ScriptSchemaObjectBase schemaObj)
        {
            return $"{schemaObj.Urn.Type.ToLowerInvariant()}.{schemaObj.AsIdentifier()}";
        }

        if (obj.ParentCollection?.ParentInstance is ScriptSchemaObjectBase parentObj)
        {
            // If we use the parent to fully qualify the name, we also include the
            // type of the object (Urn.Type) because otherwise it might not be unique.
            var sb = new ValueStringBuilder(stackalloc char[128]);
            sb.Append(parentObj.GetModelId());
            sb.Append('.');
            sb.Append(obj.Urn.Type.ToLowerInvariant());
            sb.Append('.');
            sb.Append(obj.Name.ToLowerInvariant());
            return sb.ToString().AsIdentifier();
        }

        return $"{obj.Urn.Type.ToLowerInvariant()}.{obj.AsIdentifier()}";
    }

    public static string PrettyName(this DataType dt)
    {
        ArgumentNullException.ThrowIfNull(dt);

        var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append(dt.Name.ToUpperInvariant());

        if (dt.SqlDataType == SqlDataType.NVarCharMax ||
            dt.SqlDataType == SqlDataType.VarCharMax ||
            dt.SqlDataType == SqlDataType.VarBinaryMax)
        {
            sb.Append("(MAX)");
        }
        else if (dt.SqlDataType == SqlDataType.NVarChar ||
            dt.SqlDataType == SqlDataType.VarChar ||
            dt.SqlDataType == SqlDataType.VarBinary ||
            dt.SqlDataType == SqlDataType.Char ||
            dt.SqlDataType == SqlDataType.NChar ||
            dt.SqlDataType == SqlDataType.Binary)
        {
            sb.Append("(");
            sb.AppendSpanFormattable(dt.MaximumLength);
            sb.Append(")");
        }

        return sb.ToString();
    }

    public static string ToCamelCase(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return string.Empty;

        Span<char> result = input.Length < 255 ? stackalloc char[input.Length] : new char[input.Length];
        int resultIndex = 0;

        bool lastWasUpper = false;
        bool lastWasLower = false;

        for (int i = 0; i < input.Length; i++)
        {
            char current = input[i];
            char next = i + 1 < input.Length ? input[i + 1] : '\0';

            if (i == 0)
            {
                result[resultIndex++] = char.ToLower(current);
                lastWasLower = char.IsLower(current);
                lastWasUpper = char.IsUpper(current);
                continue;
            }

            if (char.IsUpper(current))
            {
                if (lastWasUpper)
                {
                    // Multiple uppercase sequence: check if this is the last in the sequence.
                    if (char.IsLower(next))
                    {
                        result[resultIndex++] = current;
                        lastWasLower = false;
                        lastWasUpper = true;
                    }
                    else
                    {
                        result[resultIndex++] = char.ToLower(current);
                        lastWasLower = false;
                        lastWasUpper = true;
                    }
                }
                else
                {
                    // Single uppercase character, keep as uppercase.
                    result[resultIndex++] = current;
                    lastWasLower = false;
                    lastWasUpper = true;
                }
            }
            else
            {
                // Lowercase character, just append.
                result[resultIndex++] = current;
                lastWasLower = true;
                lastWasUpper = false;
            }
        }

        return new string(result.Slice(0, resultIndex));
    }

    public static bool IsNumeric(this Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                return true;
        }

        if (type == typeof(Half))
        {
            return true;
        }

        return false;
    }
}
