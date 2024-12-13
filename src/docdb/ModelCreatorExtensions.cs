using DocDB.Contracts;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Reflection.Metadata;
using System.Text;
using YamlDotNet.Serialization;

namespace DocDB;

internal static class ModelCreatorExtensions
{
    public static string GetModelId(this NamedSmoObject obj)
    {
        if (obj is IndexedColumn ic)
        {
            // Fully qualify index columns with index id to prevent duplicates.
            var sb = new ValueStringBuilder(stackalloc char[128]);
            sb.Append(ic.Parent.GetModelId());
            sb.Append('.');
            sb.Append(obj.Urn.Type.ToLowerInvariant());
            sb.Append('.');
            sb.Append(obj.Name.ToLowerInvariant());
            return sb.ToString().AsIdentifier();
        }

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

    public static string GetSyntax(this UserDefinedFunction prog)
    {
        var sb = new ValueStringBuilder(stackalloc char[255]);

        sb.AppendLine();
        sb.Append(prog.Schema);
        sb.Append('.');
        sb.Append(prog.Name);
        sb.Append('(');

        for (int i = 0; i < prog.Parameters.Count; i++)
        {
            var parameter = prog.Parameters[i];
            bool hasDefault = !string.IsNullOrEmpty(parameter.DefaultValue);
            if (hasDefault)
            {
                sb.Append("[ ");
            }

            if (parameter.Name.StartsWith('@'))
            {
                sb.Append(parameter.Name.AsSpan().Slice(1));
            }
            else
            {
                sb.Append(parameter.Name);
            }

            if (hasDefault)
            {
                sb.Append(" ]");
            }

            if (i + 1 < prog.Parameters.Count)
            {
                sb.Append(", ");
            }
        }

        sb.Append(')');


        return sb.ToString();
    }

    public static string GetSyntax(this StoredProcedure prog)
    {
        var sb = new ValueStringBuilder(stackalloc char[255]);

        sb.AppendLine();
        sb.Append(prog.Schema);
        sb.Append('.');
        sb.Append(prog.Name);

        bool hasMultiple = prog.Parameters.Count > 1;

        for (int i = 0; i < prog.Parameters.Count; i++)
        {
            var parameter = prog.Parameters[i];
            if (hasMultiple)
            {
                if (i == 0)
                {
                    sb.AppendLine();
                }
                sb.Append("   ");
            }
            else
            {
                sb.Append(' ');
            }

            bool hasDefault = !string.IsNullOrEmpty(parameter.DefaultValue);
            if (hasDefault)
            {
                sb.Append("[ ");
            }

            sb.Append(" [ ");
            sb.Append(parameter.Name);
            sb.Append(" ] = ");

            if (hasDefault)
            {
                sb.Append('\'');
                sb.Append(parameter.DefaultValue);
                sb.Append('\'');
            }
            else
            {
                if (parameter.Name.StartsWith('@'))
                {
                    sb.Append(parameter.Name.AsSpan().Slice(1));
                }
                else
                {
                    sb.Append('p');
                    sb.Append(parameter.Name);
                }
            }

            if (parameter.IsOutputParameter)
            {
                sb.Append(" OUTPUT");
            }

            if (hasDefault)
            {
                sb.Append(" ]");
            }

            if (i + 1 < prog.Parameters.Count)
            {
                sb.Append(" ,");
            }

            sb.AppendLine();
        }

        if (prog.Parameters.Count == 0)
        {
            sb.AppendLine();
        }

        sb.Append("[ ; ]");
        sb.AppendLine();

        return sb.ToString();
    }
}
