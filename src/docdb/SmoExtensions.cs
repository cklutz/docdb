using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Buffers;
using System.IO;
using System.Linq;

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
        if (obj is ScriptSchemaObjectBase schemaObj)
        {
            return quote ? $"[{schemaObj.Schema}].[{schemaObj.Name}]" : $"{schemaObj.Schema}.{schemaObj.Name}";
        }
        return quote ? $"[{obj.Name}]" : obj.Name;
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