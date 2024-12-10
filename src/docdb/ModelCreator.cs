using DocDB.Contracts;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace DocDB;

internal class ModelCreator
{
    public DdbObject? CreateObject(NamedSmoObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj switch
        {
            Schema schema => CreateSchema(schema),
            Table table => CreateTable(table),
            View view => CreateView(view),
            UserDefinedFunction udf => CreateUserDefinedFunction(udf),
            StoredProcedure sp => CreateStoredProcedure(sp),
            _ => throw new ArgumentOutOfRangeException(nameof(obj), obj.GetType(), null)
        };
    }

    private DdbSchema CreateSchema(Schema schema)
    {
        var result = new DdbSchema()
        {
            Id = schema.GetModelId(),
            Name = schema.GetFullName()
        };

        return result;
    }

    private DdbUserDefinedFunction CreateUserDefinedFunction(UserDefinedFunction udf)
    {
        var result = new DdbUserDefinedFunction
        {
            Id = udf.GetModelId(),
            Name = udf.GetFullName(),
            Description = udf.GetMSDescription(),
            CreatedAt = udf.CreateDate,
            LastModifiedAt = udf.DateLastModified,
            ReturnsNullOnNullInput = udf.ReturnsNullOnNullInput,
            FunctionType = udf.FunctionType switch
            {
                UserDefinedFunctionType.Scalar => DdbUserDefinedFunctionType.Scalar,
                UserDefinedFunctionType.Table => DdbUserDefinedFunctionType.Table,
                UserDefinedFunctionType.Inline => DdbUserDefinedFunctionType.Inline,
                _ => DdbUserDefinedFunctionType.Unknown,
            }
        };

        foreach (UserDefinedFunctionParameter parameter in udf.Parameters)
        {
            result.Parameters.Add(new()
            {
                Id = parameter.GetModelId(),
                Name = parameter.Name,
                DataType = parameter.DataType.PrettyName(),
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            });
        }

        return result;
    }

    private DdbStoredProcedure CreateStoredProcedure(StoredProcedure sp)
    {
        var result = new DdbStoredProcedure
        {
            Id = sp.GetModelId(),
            Name = sp.GetFullName(),
            Description = sp.GetMSDescription(),
            CreatedAt = sp.CreateDate,
            LastModifiedAt = sp.DateLastModified
        };

        foreach (StoredProcedureParameter parameter in sp.Parameters)
        {
            result.Parameters.Add(new()
            {
                Id = parameter.GetModelId(),
                Name = parameter.Name,
                DataType = parameter.DataType.PrettyName(),
                IsOutputParameter = parameter.IsOutputParameter,
                IsCursorParameter = parameter.IsCursorParameter,
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            });
        }

        return result;
    }


    private DdbView CreateView(View view)
    {
        var result = new DdbView
        {
            Id = view.GetModelId(),
            Name = view.GetFullName(),
            Description = view.GetMSDescription(),
            CreatedAt = view.CreateDate,
            LastModifiedAt = view.DateLastModified,
        };

        foreach (Column column in view.Columns)
        {
            result.Columns.Add(new()
            {
                Id = column.GetModelId(),
                Name = column.GetFullName(),
                Description = column.GetMSDescription(),
                IsComputed = column.Computed,
                ComputedText = column.ComputedText,
                DataType = column.DataType.PrettyName(),
                MaxLengthBytes = column.DataType.MaximumLength,
                Precision = column.DataType.NumericPrecision,
                Scale = column.DataType.NumericScale,
                IsIdentity = column.Identity,
                IdentityIncrement = column.IdentityIncrement,
                IdentitySeed = column.IdentitySeed,
                Default = column.DefaultConstraint?.Text
            });
        }

        return result;
    }

    private DdbTable CreateTable(Table table)
    {
        var result = new DdbTable()
        {
            Id = table.GetModelId(),
            Name = table.GetFullName(),
            Description = table.GetMSDescription(),
            CreatedAt = table.CreateDate,
            LastModifiedAt = table.DateLastModified,
        };

        if (table.IsPartitioned)
        {
            result.PartitionInfo.IsPartitioned = true;
            result.PartitionInfo.PartitionScheme = table.PartitionScheme;
            result.PartitionInfo.Columns.AddRange(table.PartitionSchemeParameters.OfType<PartitionSchemeParameter>().Select(x => x.Name));
            result.PartitionInfo.FileGroup = table.FileGroup;
        }

        foreach (Column column in table.Columns)
        {
            result.Columns.Add(new()
            {
                Id = column.GetModelId(),
                Name = column.GetFullName(),
                Description = column.GetMSDescription(),
                IsComputed = column.Computed,
                ComputedText = column.ComputedText,
                DataType = column.DataType.PrettyName(),
                MaxLengthBytes = column.DataType.MaximumLength,
                Precision = column.DataType.NumericPrecision,
                Scale = column.DataType.NumericScale,
                IsIdentity = column.Identity,
                IdentityIncrement = column.IdentityIncrement,
                IdentitySeed = column.IdentitySeed,
                Default = column.DefaultConstraint?.Text
            });
        }

        return result;
    }
}

internal static class ModelExtensions
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
