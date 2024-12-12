using DocDB.Contracts;
using Microsoft.SqlServer.Management.HadrModel;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;

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
            result.Columns.Add(InitBase(CreateColumn<DdbViewColumn>(column), column));
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

        foreach (ForeignKey foreignKey in table.ForeignKeys)
        {
            result.ForeignKeys.Add(InitBase(new DdbForeignKey
            {
                Columns = foreignKey.Columns.OfType<ForeignKeyColumn>().Select(c => CreateColumnRef(table, c.Name)).ToList(),
                IsChecked = foreignKey.IsChecked,
                ReferencedKey = foreignKey.ReferencedKey,
                ReferencedTable = CreateTableRef(table.Parent, foreignKey.ReferencedTableSchema, foreignKey.ReferencedTable)
            }, foreignKey));
        }

        foreach (Column column in table.Columns)
        {
            var ddbCol = InitBase(CreateColumn<DdbTableColumn>(column), column);
            if (ddbCol.IsForeignKey)
            {
                ddbCol.ForeignKeys = [.. result.ForeignKeys.Where(fk => fk.Columns.Any(c => c.Id == ddbCol.Id)).Select(fk => fk.ToRef())];
            }

            result.Columns.Add(ddbCol);
        }

        return result;
    }

    private static NamedDdbRef CreateColumnRef(Table table, string columnName)
    {
        var column = table.FindColumnByName(columnName);
        return new NamedDdbRef
        {
            Id = column.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbTableColumn>(isRef: true),
            Name = column.Name
        };
    }

    private static NamedDdbRef CreateTableRef(Database database, string schemaName, string tableName)
    {
        var table = database.FindTableByName(schemaName, tableName);

        return new NamedDdbRef
        {
            Id = table.GetModelId(),
            Type = DdbObject.GetTypeTag<DdbTable>(isRef: true),
            Name = table.GetFullName()
        };
    }

    private static T CreateColumn<T>(Column column) where T : DdbColumnBase, new()
    {
        //if (column.IsForeignKey)
        //{
        //    using var dt = column.EnumForeignKeys();
        //    var columns = dt.Columns;
        //    Console.WriteLine("column: " + column.Name);
        //    foreach (System.Data.DataRow row in dt.Rows)
        //    {
        //        var sb = new StringBuilder();
        //        foreach (System.Data.DataColumn col in columns)
        //        {
        //            sb.Append(row[col.Ordinal]);
        //            sb.Append(' ');
        //        }
        //        Console.WriteLine("\t" + sb);
        //    }
        //}

        return new T()
        {
            IsNullable = column.Nullable,
            IsComputed = column.Computed,
            ComputedText = column.ComputedText,
            DataType = column.DataType.PrettyName(),
            MaxLengthBytes = column.DataType.MaximumLength,
            Precision = column.DataType.NumericPrecision,
            Scale = column.DataType.NumericScale,
            IsIdentity = column.Identity,
            IdentityIncrement = column.IdentityIncrement,
            IdentitySeed = column.IdentitySeed,
            Default = column.DefaultConstraint?.Text,
            InPrimaryKey = column.InPrimaryKey,
            IsForeignKey = column.IsForeignKey,
            Collation = column.Collation,
            IsFileStream = column.IsFileStream,

        };
    }
}
