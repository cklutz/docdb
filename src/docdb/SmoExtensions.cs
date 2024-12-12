using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;

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

    public static string GetFullName(this NamedSmoObject obj, bool quote = true)
    {
        string? schemaName = null;
        string objectName = obj.Name;

        if (obj is ScriptSchemaObjectBase schemaObj)
        {
            schemaName = schemaObj.Name;
        }

        if (obj is Trigger trigger && trigger.Parent is Table parentTable)
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

    public static string GetLevel1ObjectTypeName(this ExtendedPropertiesLevel1ObjectTypes objectType)
    {
        switch (objectType)
        {
            case ExtendedPropertiesLevel1ObjectTypes.None:
                return "DEFAULT";
            case ExtendedPropertiesLevel1ObjectTypes.Aggregate:
            case ExtendedPropertiesLevel1ObjectTypes.Function:
            case ExtendedPropertiesLevel1ObjectTypes.Procedure:
            case ExtendedPropertiesLevel1ObjectTypes.Queue:
            case ExtendedPropertiesLevel1ObjectTypes.Rule:
            case ExtendedPropertiesLevel1ObjectTypes.Synonym:
            case ExtendedPropertiesLevel1ObjectTypes.Table:
            case ExtendedPropertiesLevel1ObjectTypes.Type:
            case ExtendedPropertiesLevel1ObjectTypes.View:
                return objectType.ToString().ToUpperInvariant();
            case ExtendedPropertiesLevel1ObjectTypes.LogicalFileName:
                return "LOGICAL FILE NAME";
            case ExtendedPropertiesLevel1ObjectTypes.XmlSchemaCollection:
                return "XML SCHEMA COLLECTION";
        }

        throw new ArgumentOutOfRangeException(nameof(objectType), objectType, null);
    }

    public static string GetLevel2ObjectTypeName(this ExtendedPropertiesLevel2ObjectTypes objectType)
    {
        switch (objectType)
        {
            case ExtendedPropertiesLevel2ObjectTypes.None:
                return null;
            case ExtendedPropertiesLevel2ObjectTypes.Column:
            case ExtendedPropertiesLevel2ObjectTypes.Constraint:
            case ExtendedPropertiesLevel2ObjectTypes.Index:
            case ExtendedPropertiesLevel2ObjectTypes.Parameter:
            case ExtendedPropertiesLevel2ObjectTypes.Trigger:
                return objectType.ToString().ToUpperInvariant();
            case ExtendedPropertiesLevel2ObjectTypes.EventNotification:
                return "EVENT NOTIFICATION";
        }

        throw new ArgumentOutOfRangeException(nameof(objectType), objectType, null);
    }

    public static string GetLevel1ObjectTypeName(this Urn urn)
    {
        ArgumentNullException.ThrowIfNull(urn);

        switch (urn.GetUrnObjectType())
        {
            case UrnObjectType.StoredProcedure:
                return "PROCEDURE";
            case UrnObjectType.UserDefinedFunction:
                return "FUNCTION";
            case UrnObjectType.UserDefinedAggregate:
                return "AGGREGATE";
            case UrnObjectType.UserDefinedDataType:
            case UrnObjectType.UserDefinedType:
                return "TYPE";
            case UrnObjectType.XmlSchemaCollection:
                return "XML SCHEMA COLLECTION";
        }

        // This might be correct or not, but until further
        // investigation it works at least for (table and view)
        return urn.Type.ToUpperInvariant();
    }

    public static Table FindTableByName(this Database database, string schemaName, string tableName)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentException.ThrowIfNullOrEmpty(tableName);

        var tables = database.Tables.OfType<Table>();
        var table = tables.FirstOrDefault(t =>
            t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
            t.Schema.Equals(schemaName, StringComparison.OrdinalIgnoreCase));
        if (table == null)
        {
            throw new InvalidOperationException($"Database {database.Name} does not contain table {schemaName}.{table}.");
        }
        return table;
    }

    public static Column FindColumnByName(this Table table, string name)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var columns = table.Columns.OfType<Column>();
        var column = columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (column == null)
        {
            throw new InvalidOperationException($"Table {table.GetFullName()} does not contain column {name}. " +
                $"Valid columns are: {string.Join(", ", columns.Select(c => c.Name))}.");
        }
        return column;
    }

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

public enum ExtendedPropertiesLevel2ObjectTypes
{
    None,
    Column,
    Constraint,
    Index,
    Parameter,
    Trigger,
    EventNotification
}

public enum ExtendedPropertiesLevel1ObjectTypes
{
    None,
    Aggregate,
    Function,
    LogicalFileName,
    Procedure,
    Queue,
    Rule,
    Synonym,
    Table,
    Type,
    View,
    XmlSchemaCollection
}