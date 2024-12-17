using DocDB.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Buffers;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using SmoIndex = Microsoft.SqlServer.Management.Smo.Index;

namespace DocDB;

public static class SmoExtensions
{
    internal const string MSDescriptionPropertyName = "MS_Description";

    public static bool IsPublicRole(this DatabaseRole role) => "public".Equals(role.Name, StringComparison.OrdinalIgnoreCase);

    // Compare to output of "SELECT [name],[schema_id] FROM [sys].[schemas]. Not documented, but also used by other SMO classes.
    public static bool IsSystemSchema(this Schema schema) => schema.ID <= 4 || (schema.ID >= 16384 && schema.ID < 16400);

    public static ScriptingOptions WithOutputFileName(this ScriptingOptions options, string outputFileName)
    {
        if (options.FileName == outputFileName)
        {
            return options;
        }

        return new ScriptingOptions(options) { FileName = outputFileName };
    }

    public static ScriptingOptions WithAllowSystemObjects(this ScriptingOptions options, bool value)
    {
        if (options.AllowSystemObjects == value)
        {
            return options;
        }

        return new ScriptingOptions(options) { AllowSystemObjects = value };
    }

    public static string GetScript(this SmoObjectBase obj, ScriptingOptions? scriptingOptions = null)
    {
        if (obj is IScriptable scriptable)
        {
            scriptingOptions ??= new();
            var batches = scriptable.Script(scriptingOptions);
            var output = new StringBuilder();

            foreach (string? batch in batches)
            {
                // Ensure that lines to get overly long. For example when scripting "CREATE ASSEMBLY ...".
                // By using 255 we leave a little leeway. Because the wrapping algorithm does not handle
                // quotes, etc. we need to make sure the likeliness, that wrapping occurs inside a string
                // literal, etc. is reduced.
                if (batch?.Length > 255)
                {
                    WrapLines(output, batch, 255);
                }
                else
                {
                    output.Append(batch.AsSpan().TrimStart(NewLines).TrimEnd());
                    output.AppendLine();
                }
                output.AppendLine("GO");
                output.AppendLine();
            }

            return output.ToString();
        }

        return "";
    }

    private static ReadOnlySpan<char> NewLines => ['\r', '\n'];
    private static readonly SearchValues<char> s_wrapAt = SearchValues.Create([' ', '\t', '.', ',', ';', ':', '!', '?']);
    private static readonly SearchValues<char> s_space = SearchValues.Create([' ', '\t']);

    private static void WrapLines(StringBuilder buffer, string input, int maxLineLength)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLineLength, 0);

        var span = input.AsSpan().TrimStart(NewLines);
        int count = span.Count('\n') + 1;
        Span<Range> ranges = count <= 255 ? stackalloc Range[count] : new Range[count];
        int num = span.Split(ranges, '\n');
        for (int i = 0; i < num; i++)
        {
            WrapLine(buffer, span[ranges[i]].TrimEnd(), maxLineLength);
        }
    }

    private static void WrapLine(StringBuilder buffer, ReadOnlySpan<char> inputSpan, int maxLineLength)
    {
        if (inputSpan.Length < maxLineLength)
        {
            buffer.Append(inputSpan.TrimEnd());
            buffer.AppendLine();
            return;
        }

        while (!inputSpan.IsEmpty)
        {
            int currentLineLength = Math.Min(maxLineLength, inputSpan.Length);
            int wrapIndex = currentLineLength;

            // Find a preferred break point before the limit
            if (wrapIndex < inputSpan.Length && !s_wrapAt.Contains(inputSpan[wrapIndex]))
            {
                int lastPreferredIndex = inputSpan.Slice(0, currentLineLength).LastIndexOfAny(s_wrapAt);
                if (lastPreferredIndex >= 0)
                {
                    wrapIndex = lastPreferredIndex + 1;
                }
                else
                {
                    // No preferred break point found; fall back to word boundary
                    int nextWhitespace = inputSpan.Slice(wrapIndex).IndexOfAny(s_space);
                    if (nextWhitespace >= 0)
                    {
                        wrapIndex += nextWhitespace; // Extend to the next whitespace
                    }
                }
            }

            // Append the line to the result
            buffer.Append(inputSpan.Slice(0, wrapIndex).TrimEnd());
            buffer.AppendLine();

            // Move the span forward
            inputSpan = inputSpan.Slice(wrapIndex).TrimStart();
        }
    }

    public static string AsIdentifier(this NamedSmoObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return obj.GetFullName(quote: false).AsIdentifier();
    }

    public static string AsIdentifier(this string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        return MakeIdentifierSafe(str).ToLowerInvariant();
    }

    // Identifiers are like path names, but even stricter.
    private static readonly SearchValues<char> s_invalidFileNameChars = SearchValues.Create(Path.GetInvalidFileNameChars().Concat([' ', '\t']).ToArray());
    private static string MakeIdentifierSafe(string str)
    {
        var span = str.AsSpan();
        int pos = span.IndexOfAny(s_invalidFileNameChars);
        if (pos == -1)
        {
            return str;
        }

        Span<char> chars = str.Length <= 255 ? stackalloc char[str.Length] : new char[str.Length];
        str.CopyTo(chars);

        var remaining = chars;
        while (pos >= 0)
        {
            remaining[pos] = '_';
            remaining = remaining.Slice(pos + 1);
            pos = remaining.IndexOfAny(s_invalidFileNameChars);
        }

        return new string(chars);
    }

    public static string GetFullName(string? serverName, string? baseDatabase, string? schemaName, string objectName)
    {
        var sb = new ValueStringBuilder(stackalloc char[128]);

        if (!string.IsNullOrEmpty(serverName))
        {
            sb.Append('[');
            sb.Append(serverName);
            sb.Append(']');
            sb.Append('.');
        }

        if (!string.IsNullOrEmpty(baseDatabase))
        {
            sb.Append('[');
            sb.Append(baseDatabase);
            sb.Append(']');
            sb.Append('.');
        }

        if (!string.IsNullOrEmpty(schemaName))
        {
            sb.Append('[');
            sb.Append(schemaName);
            sb.Append(']');
            sb.Append('.');
        }

        sb.Append('[');
        sb.Append(objectName);
        sb.Append(']');

        return sb.ToString();
    }

    public static string GetFullName(this NamedSmoObject obj, bool quote = true)
    {
        string? schemaName = null;
        string objectName = obj.Name;

        if (obj is ScriptSchemaObjectBase schemaObj)
        {
            schemaName = schemaObj.Schema;
        }
        else if (obj is Trigger trigger && trigger.Parent is Table parentTable)
        {
            schemaName = parentTable.Schema;
        }

        if (!string.IsNullOrEmpty(schemaName))
        {
            return quote ? $"[{schemaName}].[{objectName}]" : $"{schemaName}.{objectName}";
        }

        return quote ? $"[{objectName}]" : objectName;
    }

    public static UrnObjectType GetUrnObjectType(this Urn urn)
    {
        ArgumentNullException.ThrowIfNull(urn);

        try
        {
            return (UrnObjectType)Enum.Parse(typeof(UrnObjectType), urn.Type);
        }
        catch (ArgumentException)
        {
            return UrnObjectType.Unknown;
        }
    }

    public static string? GetMSDescription(this IExtendedProperties obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        // Seen this with "Index": we get exceptions attempting to access the extended
        // properties for "system named" ones.
        if (obj is SqlSmoObject sql && sql.IsSupportedProperty("IsSystemNamed") &&
            sql.Properties["IsSystemNamed"]?.Value is bool isSystemNamed &&
            isSystemNamed)
        {
            return null;
        }

        if (obj.ExtendedProperties != null && obj.ExtendedProperties.Contains(MSDescriptionPropertyName))
        {
            object val = obj.ExtendedProperties[MSDescriptionPropertyName].Value;
            if (val != null)
            {
                return val.ToString();
            }
        }

        return null;
    }

    public static Database GetDatabase(this SmoObjectBase obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (obj is View view)
        {
            return view.Parent;
        }

        if (obj is Table table)
        {
            return table.Parent;
        }

        if (obj is UserDefinedTableType tableType)
        {
            return tableType.Parent;
        }

        if (obj is SmoIndex index)
        {
            // Should descend into "UserDefinedTableType", "View" or "Table" cases above.
            return index.Parent.GetDatabase();
        }

        throw new ArgumentException($"Unexpected type {obj.GetType()}.");
    }

    public static DateTime GetLastModificationDate(this Database database)
    {
        ArgumentNullException.ThrowIfNull(database);

        var connection = database.Parent?.ConnectionContext;
        if (connection == null)
        {
            throw new InvalidOperationException("No connection context.");
        }

        var dt = (DateTime)connection.ExecuteScalar("SELECT TOP(1) modify_date FROM sys.objects where is_ms_shipped = 0 ORDER BY modify_date DESC");
        return dt;
    }

    public static Default FindDefaultByName(this Database database, string schemaName, string defaultName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentException.ThrowIfNullOrEmpty(defaultName);

        var @default = database.Defaults.FindFirstOrDefault<Default>(schemaName, defaultName);
        if (@default == null)
        {
            throw new InvalidOperationException($"Database {database.Name} does not contain default {schemaName}.{defaultName}.");
        }
        return @default;
    }

    public static Rule FindRuleByName(this Database database, string schemaName, string ruleName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentException.ThrowIfNullOrEmpty(ruleName);

        var @rule = database.Rules.FindFirstOrDefault<Rule>(schemaName, ruleName);
        if (@rule == null)
        {
            throw new InvalidOperationException($"Database {database.Name} does not contain rule {schemaName}.{ruleName}.");
        }
        return @rule;
    }

    public static FileGroup FindFileGroupByName(this Database database, string name)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var fileGroup = database.FileGroups.FindFirstOrDefault<FileGroup>(name);
        if (fileGroup == null)
        {
            throw new InvalidOperationException($"Database {database.Name} does not contain file group {name}.");
        }
        return fileGroup;
    }

    public static Table FindTableByName(this Database database, string schemaName, string tableName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentException.ThrowIfNullOrEmpty(tableName);

        var table = database.Tables.FindFirstOrDefault<Table>(schemaName, tableName);
        if (table == null)
        {
            throw new InvalidOperationException($"Database {database.Name} does not contain table {schemaName}.{table}.");
        }
        return table;
    }

    public static Column FindColumnByName(this TableViewTableTypeBase table, string name)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var column = table.Columns.FindFirstOrDefault<Column>(name);
        if (column == null)
        {
            throw new InvalidOperationException($"Table {table.GetFullName()} does not contain column {name}.");
        }
        return column;
    }

    public static T? FindFirstOrDefault<T>(this SmoCollectionBase source, string name) where T : NamedSmoObject =>
        source.OfType<T>().FirstOrDefault(o => AreSame(o, name));

    public static T? FindFirstOrDefault<T>(this SmoCollectionBase source, string schemaName, string objectName) where T : ScriptSchemaObjectBase =>
        source.OfType<T>().FirstOrDefault(o => AreSame(o, schemaName, objectName));

    public static bool AreSame(NamedSmoObject obj1, NamedSmoObject obj2) =>
        obj1.GetStringComparer().Compare(obj1.Name, obj2.Name) == 0;

    public static bool AreSame(NamedSmoObject obj1, string name) =>
        obj1.GetStringComparer().Compare(obj1.Name, name) == 0;

    public static bool AreSame(ScriptSchemaObjectBase obj1, ScriptSchemaObjectBase obj2) =>
        obj1.GetStringComparer().Compare(obj1.Schema, obj2.Schema) == 0 &&
        obj1.GetStringComparer().Compare(obj1.Name, obj2.Name) == 0;

    public static bool AreSame(ScriptSchemaObjectBase obj1, string schemaName, string objectName) =>
        obj1.GetStringComparer().Compare(obj1.Schema, schemaName) == 0 &&
        obj1.GetStringComparer().Compare(obj1.Name, objectName) == 0;

    public static string ToQuotedName(string arg0) => DoCreateQuotedName(arg0, null, null, null);
    public static string ToQuotedName(string arg0, string arg1) => DoCreateQuotedName(arg0, arg1, null, null);
    public static string ToQuotedName(string arg0, string arg1, string arg2) => DoCreateQuotedName(arg0, arg1, arg2, null);
    public static string ToQuotedName(string arg0, string arg1, string arg2, string arg3) => DoCreateQuotedName(arg0, arg1, arg2, arg3);
    private static string DoCreateQuotedName(string arg0, string? arg1, string? arg2, string? arg3)
    {
        ArgumentNullException.ThrowIfNull(arg0);

        var sb = new ValueStringBuilder(stackalloc char[255]);
        AddArg(ref sb, arg0);
        if (arg1 != null)
        {
            AddArg(ref sb, arg1);
            if (arg2 != null)
            {
                AddArg(ref sb, arg2);
                if (arg3 != null)
                {
                    AddArg(ref sb, arg3);
                }
            }
        }

        return sb.ToString();

        static void AddArg(ref ValueStringBuilder sb, string arg)
        {
            if (sb.Length > 0)
            {
                sb.Append('.');
            }

            if (arg.Length > 0)
            {
                sb.Append('[');
                sb.Append(arg);
                sb.Append(']');
            }
        }
    }

    public static string GetTypeTag(this SynonymBaseType baseType)
    {
        return baseType switch
        {
            SynonymBaseType.Table => DdbObject.GetTypeTag<DdbTable>(isRef: true),
            SynonymBaseType.View => DdbObject.GetTypeTag<DdbView>(isRef: true),
            SynonymBaseType.SqlStoredProcedure => DdbObject.GetTypeTag<DdbStoredProcedure>(isRef: true),
            SynonymBaseType.SqlScalarFunction => DdbObject.GetTypeTag<DdbUserDefinedFunction>(isRef: true),
            SynonymBaseType.SqlTableValuedFunction => DdbObject.GetTypeTag<DdbUserDefinedFunction>(isRef: true),
            SynonymBaseType.SqlInlineTableValuedFunction => DdbObject.GetTypeTag<DdbUserDefinedFunction>(isRef: true),
            SynonymBaseType.ClrStoredProcedure => DdbObject.GetTypeTag<DdbStoredProcedure>(isRef: true),
            SynonymBaseType.ClrScalarFunction => DdbObject.GetTypeTag<DdbUserDefinedFunction>(isRef: true),
            SynonymBaseType.ClrTableValuedFunction => DdbObject.GetTypeTag<DdbUserDefinedFunction>(isRef: true),
            SynonymBaseType.ClrAggregateFunction => DdbObject.GetTypeTag<DdbUserDefinedAggregate>(isRef: true),
            SynonymBaseType.ExtendedStoredProcedure => "",
            SynonymBaseType.ReplicationFilterProcedure => "",
            _ => "",
        };
    }

    public static NamedDdbRef? FindBaseRef(this Synonym synonym)
    {
        NamedDdbRef? baseRef = null;
        switch (synonym.BaseType)
        {
            case SynonymBaseType.Table:
                baseRef = synonym.Parent.Tables.FindFirstOrDefault<Table>(synonym.BaseSchema, synonym.BaseObject)?.ToNamedRef<DdbTable>();
                break;
            case SynonymBaseType.View:
                baseRef = synonym.Parent.Views.FindFirstOrDefault<View>(synonym.BaseSchema, synonym.BaseObject)?.ToNamedRef<DdbView>();
                break;
            case SynonymBaseType.SqlStoredProcedure:
            case SynonymBaseType.ClrStoredProcedure:
                baseRef = synonym.Parent.StoredProcedures.FindFirstOrDefault<StoredProcedure>(synonym.BaseSchema, synonym.BaseObject)?.ToNamedRef<DdbStoredProcedure>();
                break;
            case SynonymBaseType.SqlScalarFunction:
            case SynonymBaseType.SqlInlineTableValuedFunction:
            case SynonymBaseType.ClrScalarFunction:
            case SynonymBaseType.SqlTableValuedFunction:
            case SynonymBaseType.ClrTableValuedFunction:
                baseRef = synonym.Parent.UserDefinedFunctions.FindFirstOrDefault<UserDefinedFunction>(synonym.BaseSchema, synonym.BaseObject)?.ToNamedRef<DdbUserDefinedFunction>();
                break;
            case SynonymBaseType.ReplicationFilterProcedure:
                break;
            case SynonymBaseType.ClrAggregateFunction:
                baseRef = synonym.Parent.UserDefinedAggregates.FindFirstOrDefault<UserDefinedAggregate>(synonym.BaseSchema, synonym.BaseObject)?.ToNamedRef<DdbUserDefinedAggregate>();
                break;
        }
        return baseRef;
    }
}

public enum UrnObjectType
{
    // NOTE: The "Names" below are aligned with the value of "Type" property
    // of the Urn class.
    Unknown,
    Table,
    View,
    StoredProcedure,
    UserDefinedFunction,
    UserDefinedAggregate,
    UserDefinedDataType,
    UserDefinedType,
    UserDefinedMessage,
    XmlSchemaCollection,
    SqlAssembly,
    PartitionFunction,
    PartitionScheme,
    Role,
    ApplicationRole,
    Schema,
    DatabaseDdlTrigger
}
