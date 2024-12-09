using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocDB.Model;

public abstract class DdbObject
{
    public DdbObject(string id)
    {
        Id = id;
    }

    public string Id { get; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }

    public override bool Equals(object? obj) => obj is DdbObject dbo && dbo.Id == Id;
    public override int GetHashCode() => Id.GetHashCode();
    public override string ToString() => Id;
}

public abstract class NamedDdbObject(string id, string name) : DdbObject(id)
{
    public string Name { get; } = name;
}

public class DdbSchema(string id, string name) : NamedDdbObject(id, name)
{
}

public enum DdbUserDefinedFunctionType
{
    Unknown,
    Scalar,
    Table,
    Inline
}


public abstract class DdbParameterBase(string id, string name) : NamedDdbObject(id, name)
{
    public string? DataType { get; set; }
    public string? DefaultValue { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}

public class DdbStoredProcedureParameter(string id, string name) : DdbParameterBase(id, name)
{
    public bool IsOutputParameter { get; set; }
    public bool IsCursorParameter { get; set; }
}

public class DdbUserDefinedFunctionParameter(string id, string name) : DdbParameterBase(id, name)
{
}

public abstract class DdbProcedureOrFunctionObject<TParameter>(string id, string name) : NamedDdbObject(id, name)
    where TParameter : DdbParameterBase
{
    public string? Title { get; set; }
    public List<TParameter> Parameters { get; } = [];
}

public class DdbStoredProcedure(string id, string name) : DdbProcedureOrFunctionObject<DdbStoredProcedureParameter>(id, name)
{
}

public class DbdUserDefinedAggregate(string id, string name) : NamedDdbObject(id, name)
{
}

public class DdbUserDefinedFunction(string id, string name) : DdbProcedureOrFunctionObject<DdbUserDefinedFunctionParameter>(id, name)
{
    public DdbUserDefinedFunctionType FunctionType { get; set; }
    public bool ReturnsNullOnNullInput { get; set; }
}

public abstract class DdbColumnBase(string id, string name) : NamedDdbObject(id, name)
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

public class DdbTableColumn(string id, string name) : DdbColumnBase(id, name)
{
}

public class DdbViewColumn(string id, string name) : DdbColumnBase(id, name)
{
}

public class DdbIndex(string id, string name) : NamedDdbObject(id, name)
{
    public List<string> Columns { get; } = [];
    public bool IsUnique { get; set; }
    public bool IsClustered { get; set; }
    public bool IsPartitioned { get; set; }
}

public class DdbForeignKey(string id, string name) : NamedDdbObject(id, name)
{
    public List<string> Columns { get; } = [];
    public bool NoCheck { get; set; }
}

public class DdbCheckConstraint(string id, string name) : NamedDdbObject(id, name)
{
    public string? On { get; set; }
    public string? ConstraintText { get; set; }
}

public abstract class TabularDdbObject<TColumn> : NamedDdbObject where TColumn : DdbColumnBase
{
    protected TabularDdbObject(string id, string name) : base(id, name)
    {
    }
    public string? Title { get; set; }
    public List<TColumn> Columns { get; } = [];
    public List<DdbIndex> Indexes { get; } = [];
}

public class DdbTablePartitionInfo
{
    public bool IsPartitioned { get; set; }
    public List<string> Columns { get; } = new();
    public string? PartitionScheme { get; set; }
    public string? FileGroup { get; set; }
}

public class DdbTable(string id, string name) : TabularDdbObject<DdbTableColumn>(id, name)
{
    public List<DdbForeignKey> ForeignKeys { get; } = [];
    public List<DdbCheckConstraint> Checks { get; } = [];
    public DdbTablePartitionInfo PartitionInfo { get; } = new();
}

public class DdbView(string id, string name) : TabularDdbObject<DdbViewColumn>(id, name)
{
}
