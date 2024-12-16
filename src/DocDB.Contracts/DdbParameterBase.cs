using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DocDB.Contracts;

public abstract class DdbParameterBase : NamedDdbObject
{
    [JsonPropertyName("dataType"), JsonProperty("dataType")]
    public string? DataType { get; set; }
    [JsonPropertyName("dataTypeRef"), JsonProperty("dataTypeRef")]
    public NamedDdbRef? DataTypeRef { get; set; }
    [JsonPropertyName("defaultValue"), JsonProperty("defaultValue")]
    public string? DefaultValue { get; set; }

    public override bool Equals(object? obj) => obj is DdbColumnBase dbo && dbo.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
}
