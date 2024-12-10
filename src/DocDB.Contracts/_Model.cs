using System;
using System.Collections.Generic;
using System.Data.Common;

namespace DocDB.Contracts;

public abstract class DdbObject
{
    protected DdbObject()
    {
        Type = GetTypeTag(GetType());
    }

    public string Id { get; set; } = null!;
    public string Type { get; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }

    public override bool Equals(object? obj) => obj is DdbObject dbo && dbo.Id == Id && dbo.Type == Type;
    public override int GetHashCode() => HashCode.Combine(Id, Type);
    public override string ToString() => Id;

    public static Dictionary<string, Type> GetTypeMappings()
    {
        return typeof(DdbObject).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsClass && t.IsAssignableTo(typeof(DdbObject)))
            .ToDictionary(GetTypeTag, t => t);
    }

    public static string GetTypeTag<T>() where T : DdbObject => GetTypeTag(typeof(T));

    private static string GetTypeTag(Type type)
    {
        var span = type.Name.AsSpan();
        if (span.StartsWith("Ddb"))
        {
            span = span.Slice(3);
        }
        return span.ToString();
    }
}

public abstract class NamedDdbObject : DdbObject
{
    public string Name { get; set; } = null!;
}

public class DdbSchema : NamedDdbObject
{
}

public enum DdbUserDefinedFunctionType
{
    Unknown,
    Scalar,
    Table,
    Inline
}


public abstract class DdbParameterBase : NamedDdbObject
{
    public string? DataType { get; set; }
    public string? DefaultValue { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}

public class DdbStoredProcedureParameter : DdbParameterBase
{
    public bool IsOutputParameter { get; set; }
    public bool IsCursorParameter { get; set; }
}

public class DdbUserDefinedFunctionParameter : DdbParameterBase
{
}

public abstract class DdbProcedureOrFunctionObject<TParameter> : NamedDdbObject
    where TParameter : DdbParameterBase
{
    public string? Title { get; set; }
    public List<TParameter> Parameters { get; set; } = [];
}

public class DdbStoredProcedure : DdbProcedureOrFunctionObject<DdbStoredProcedureParameter>
{
}

public class DbdUserDefinedAggregate : NamedDdbObject
{
}

public class DdbUserDefinedFunction : DdbProcedureOrFunctionObject<DdbUserDefinedFunctionParameter>
{
    public DdbUserDefinedFunctionType FunctionType { get; set; }
    public bool ReturnsNullOnNullInput { get; set; }
}

public abstract class DdbColumnBase : NamedDdbObject
{
    public string? DataType { get; set; }
    public int? MaxLengthBytes { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }

    public bool IsComputed { get; set; }
    public string? ComputedText { get; set; }
    public bool IsIdentity { get; set; }
    public long? IdentityIncrement { get; set; }
    public long? IdentitySeed { get; set; }
    public bool InPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public string? Default { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}

public class DdbTableColumn : DdbColumnBase
{
}

public class DdbViewColumn : DdbColumnBase
{
}

public class DdbIndex : NamedDdbObject
{
    public List<string> Columns { get; set; } = [];
    public bool IsUnique { get; set; }
    public bool IsClustered { get; set; }
    public bool IsPartitioned { get; set; }
}

public class DdbForeignKey : NamedDdbObject
{
    public List<string> Columns { get; set; } = [];
    public bool NoCheck { get; set; }
}

public class DdbCheckConstraint : NamedDdbObject
{
    public string? On { get; set; }
    public string? ConstraintText { get; set; }
}

public class DdbTablePartitionInfo
{
    public bool IsPartitioned { get; set; }
    public List<string> Columns { get; set; } = [];
    public string? PartitionScheme { get; set; }
    public string? FileGroup { get; set; }
}

public abstract class TabularDdbObject<TColumn> : NamedDdbObject where TColumn : DdbColumnBase
{
   public string? Title { get; set; }
    public List<DdbIndex> Indexes { get; set; } = [];
    public List<TColumn> Columns { get; set;  } = [];
}

public class DdbTable : TabularDdbObject<DdbTableColumn>
{
    public List<DdbForeignKey> ForeignKeys { get; set; } = [];
    public List<DdbCheckConstraint> Checks { get; set; } = [];
    public DdbTablePartitionInfo PartitionInfo { get; set; } = new();
}

public class DdbView : TabularDdbObject<DdbViewColumn>
{
}
