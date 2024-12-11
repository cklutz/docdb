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

    private static T InitBase<T>(T obj, NamedSmoObject smo) where T : DdbObject
    {
        obj.Id = smo.GetModelId();

        if (obj is NamedDdbObject named)
        {
            named.Name = smo.GetFullName(quote: false);
        }

        if (smo is IExtendedProperties extendedProperties)
        {
            obj.Description = extendedProperties.GetMSDescription();
        }

        if (smo is IScriptable)
        {
            var scriptingOptions = new ScriptingOptions();
            if (obj is DdbTable || obj is DdbView)
            {
                scriptingOptions.Indexes = true;
                scriptingOptions.Triggers = true;
                scriptingOptions.DriAll = true;
            }
            obj.Script = smo.GetScript(scriptingOptions);
        }

        return obj;
    }

    private DdbSchema CreateSchema(Schema schema)
    {
        var result = InitBase(new DdbSchema(), schema);
        return result;
    }

    private DdbUserDefinedFunction CreateUserDefinedFunction(UserDefinedFunction udf)
    {
        var result = InitBase(new DdbUserDefinedFunction
        {
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
        }, udf);

        foreach (UserDefinedFunctionParameter parameter in udf.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbUserDefinedFunctionParameter()
            {
                DataType = parameter.DataType.PrettyName(),
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            }, parameter));
        }

        return result;
    }

    private DdbStoredProcedure CreateStoredProcedure(StoredProcedure sp)
    {
        var result = InitBase(new DdbStoredProcedure
        {
            CreatedAt = sp.CreateDate,
            LastModifiedAt = sp.DateLastModified
        }, sp);

        foreach (StoredProcedureParameter parameter in sp.Parameters)
        {
            result.Parameters.Add(InitBase(new DdbStoredProcedureParameter()
            {
                DataType = parameter.DataType.PrettyName(),
                IsOutputParameter = parameter.IsOutputParameter,
                IsCursorParameter = parameter.IsCursorParameter,
                Description = parameter.GetMSDescription(),
                DefaultValue = parameter.DefaultValue,
            }, parameter));
        }

        return result;
    }


    private DdbView CreateView(View view)
    {
        var result = InitBase(new DdbView
        {
            CreatedAt = view.CreateDate,
            LastModifiedAt = view.DateLastModified,
        }, view);

        foreach (Column column in view.Columns)
        {
            result.Columns.Add(InitBase(new DdbViewColumn()
            {
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
            }, column));
        }

        return result;
    }

    private DdbTable CreateTable(Table table)
    {
        var result = InitBase(new DdbTable()
        {
            CreatedAt = table.CreateDate,
            LastModifiedAt = table.DateLastModified,
        }, table);

        if (table.IsPartitioned)
        {
            result.PartitionInfo.IsPartitioned = true;
            result.PartitionInfo.PartitionScheme = table.PartitionScheme;
            result.PartitionInfo.Columns.AddRange(table.PartitionSchemeParameters.OfType<PartitionSchemeParameter>().Select(x => x.Name));
            result.PartitionInfo.FileGroup = table.FileGroup;
        }

        foreach (Column column in table.Columns)
        {
            result.Columns.Add(InitBase(new DdbTableColumn()
            {
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
            }, column));
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
