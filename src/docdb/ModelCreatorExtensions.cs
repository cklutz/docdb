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
}
